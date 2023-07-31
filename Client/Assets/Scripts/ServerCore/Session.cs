using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ServerCore
{
	public class SessionTCP : PacketSession
	{
		protected override void RegisterSend()
		{
			if (_disconnected == 1)
				return;

			while (_sendQueue.Count > 0)
			{
				ArraySegment<byte> buff = _sendQueue.Dequeue();
				_pendingList.Add(buff);
			}
			_sendArgs.BufferList = _pendingList;

			try
			{
				bool pending = _socket.SendAsync(_sendArgs);
				if (pending == false)
					OnSendCompleted(null, _sendArgs);
			}
			catch (Exception e)
			{
				Console.WriteLine($"RegisterSend Failed {e}");
			}
		}


		protected override void RegisterRecv()
		{
			if (_disconnected == 1)
				return;

			_recvBuffer.Clean();
			ArraySegment<byte> segment = _recvBuffer.WriteSegment;
			_recvArgs.SetBuffer(segment.Array, segment.Offset, segment.Count);

			try
			{
				bool pending = _socket.ReceiveAsync(_recvArgs);
				if (pending == false)
					OnRecvCompleted(null, _recvArgs);
			}
			catch (Exception e)
			{
				Console.WriteLine($"RegisterRecv Failed {e}");
			}
		}
	}

	public class SessionUDP : PacketSession
	{
		protected override void RegisterSend()
		{
			if (_disconnected == 1)
				return;

			while (_sendQueue.Count > 0)
			{
				ArraySegment<byte> buff = _sendQueue.Dequeue();
				_pendingList.Add(buff);
			}
			_sendArgs.BufferList = _pendingList;
			_sendArgs.RemoteEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 7778); // Server측의 udpPort

			try
			{
				bool pending = _socket.SendToAsync(_sendArgs);
				if (pending == false)
					OnSendCompleted(null, _sendArgs);
			}
			catch (Exception e)
			{
				Console.WriteLine($"RegisterSend Failed {e}");
			}
		}


		protected override void RegisterRecv()
		{
			if (_disconnected == 1)
				return;

			// Receive()할 데이터 공간 정의
			_recvBuffer.Clean();
			ArraySegment<byte> segment = _recvBuffer.WriteSegment;
			_recvArgs.SetBuffer(segment.Array, segment.Offset, segment.Count);
			_recvArgs.RemoteEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 7778); // Server측의 udpPort

			try
			{
				bool pending = _socket.ReceiveFromAsync(_recvArgs);
				if (pending == false)
					OnRecvCompleted(null, _recvArgs);
			}
			catch (Exception e)
			{
				Console.WriteLine($"RegisterRecv Failed {e}");
			}
		}
	}

	public abstract class PacketSession : Session
	{
		public static readonly int HeaderSize = 2;

		public sealed override int OnRecv(ArraySegment<byte> buffer)
		{
			int processLen = 0;

			while (true)
			{
				if (buffer.Count < HeaderSize)
					break;

				ushort dataSize = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
				if (buffer.Count < dataSize)
					break;

				OnRecvPacket(new ArraySegment<byte>(buffer.Array, buffer.Offset, dataSize));

				processLen += dataSize;
				buffer = new ArraySegment<byte>(buffer.Array, buffer.Offset + dataSize, buffer.Count - dataSize);
			}

			return processLen;
		}

		public System.EventHandler<ArraySegment<byte>> OnRecvPacketOccur;
		public virtual void OnRecvPacket(ArraySegment<byte> buffer) { OnRecvPacketOccur?.Invoke(this, buffer); }

		public System.EventHandler<EndPoint> OnConnectedOccur;
		public System.EventHandler<int> OnSendOccur;
		public System.EventHandler<EndPoint> OnDisconnectedOccur;
		public override void OnConnected(EndPoint endPoint) { OnConnectedOccur?.Invoke(this, endPoint); }
		public override void OnSend(int numOfBytes) { OnSendOccur?.Invoke(this, numOfBytes); }
		public override void OnDisconnected(EndPoint endPoint) { OnDisconnectedOccur?.Invoke(this, endPoint); }
	}

	public abstract class Session
	{
		protected Socket _socket;
		protected int _disconnected = 0;

		protected RecvBuffer _recvBuffer = new RecvBuffer(65535);

		protected object _lock = new object();
		protected Queue<ArraySegment<byte>> _sendQueue = new Queue<ArraySegment<byte>>();
		protected List<ArraySegment<byte>> _pendingList = new List<ArraySegment<byte>>();
		protected SocketAsyncEventArgs _sendArgs = new SocketAsyncEventArgs();
		protected SocketAsyncEventArgs _recvArgs = new SocketAsyncEventArgs();
		public abstract void OnConnected(EndPoint endPoint);
		public abstract int OnRecv(ArraySegment<byte> buffer);
		public abstract void OnSend(int numOfBytes);
		public abstract void OnDisconnected(EndPoint endPoint);

		protected void Clear()
		{
			lock (_lock)
			{
				_sendQueue.Clear();
				_pendingList.Clear();
			}
		}

		public void Start(Socket socket)
		{
			_socket = socket;

			_recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecvCompleted);
			_sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);

			RegisterRecv();
		}

		public void Send(List<ArraySegment<byte>> sendBuffList)
		{
			if (sendBuffList.Count == 0)
				return;

			lock (_lock)
			{
				foreach (ArraySegment<byte> sendBuff in sendBuffList)
					_sendQueue.Enqueue(sendBuff);

				if (_pendingList.Count == 0)
					RegisterSend();
			}
		}

		public void Send(ArraySegment<byte> sendBuff)
		{
			lock (_lock)
			{
				_sendQueue.Enqueue(sendBuff);
				if (_pendingList.Count == 0)
					RegisterSend();
			}
		}

		public void Disconnect()
		{
			if (Interlocked.Exchange(ref _disconnected, 1) == 1)
				return;

			OnDisconnected(_socket.RemoteEndPoint);
			_socket.Shutdown(SocketShutdown.Both);
			_socket.Close();
			Clear();
		}

		protected abstract void RegisterSend();
		protected void OnSendCompleted(object sender, SocketAsyncEventArgs args)
		{
			lock (_lock)
			{
				if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
				{
					try
					{
						_sendArgs.BufferList = null;
						_pendingList.Clear();

						OnSend(_sendArgs.BytesTransferred);

						if (_sendQueue.Count > 0)
							RegisterSend();
					}
					catch (Exception e)
					{
						Console.WriteLine($"OnSendCompleted Failed {e}");
					}
				}
				else
				{
					Disconnect();
				}
			}
		}
		protected abstract void RegisterRecv();
		protected void OnRecvCompleted(object sender, SocketAsyncEventArgs args)
		{
			if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
			{
				try
				{
					if (_recvBuffer.OnWrite(args.BytesTransferred) == false)
					{
						Disconnect();
						return;
					}

					int processLen = OnRecv(_recvBuffer.ReadSegment);
					if (processLen < 0 || _recvBuffer.DataSize < processLen)
					{
						Disconnect();
						return;
					}

					if (_recvBuffer.OnRead(processLen) == false)
					{
						Disconnect();
						return;
					}

					RegisterRecv();
				}
				catch (Exception e)
				{
					Console.WriteLine($"OnRecvCompleted Failed {e}");
				}
			}
			else
			{
				Disconnect();
			}
		}
	}
}
