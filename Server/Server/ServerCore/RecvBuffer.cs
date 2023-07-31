using System;
using System.Collections.Generic;
using System.Text;

namespace ServerCore
{
	/// <summary>
	/// 각 스레드(클라이언트 세션)에서 Recv()한 버퍼 데이터들을 순차적으로 담아줌
	/// </summary>
	public class RecvBuffer
	{
		// 10byte 중에 2byte를 받았을 경우 => [r][][w][][][][][][][]
		ArraySegment<byte> _buffer;
		int _readPos; // 읽기 커서
		int _writePos; // 쓰기 커서

		public RecvBuffer(int bufferSize)
		{
			_buffer = new ArraySegment<byte>(new byte[bufferSize], 0, bufferSize);
		}

		public int DataSize { get { return _writePos - _readPos; } } // 유효범위 (=데이터가 쌓인정도, r~w까지의 거리)
		public int FreeSize { get { return _buffer.Count - _writePos; } } // 버퍼의 남은 공간 (=w~끝 인덱스 까지의 거리)

		public ArraySegment<byte> ReadSegment // 읽기 가능한 데이터(영역) 가져오기 (=r~w까지의 메모리) 
		{
			get { return new ArraySegment<byte>(_buffer.Array, _buffer.Offset + _readPos, DataSize); }
		}

		public ArraySegment<byte> WriteSegment // 쓰기 가능한 데이터(영역) 가져오기 (w~끝 인덱스 까지의 메모리)
		{
			get { return new ArraySegment<byte>(_buffer.Array, _buffer.Offset + _writePos, FreeSize); }
		}

		// [rw][][][][][][][][][] 커서를 다시 당김
		public void Clean()
		{
			int dataSize = DataSize;
			if (dataSize == 0)
			{
				// 남은 데이터가 없으면 복사하지 않고 커서 위치만 리셋
				_readPos = _writePos = 0;
			}
			else
			{
				// 아직 읽지 못한 데이터가 있다면 => 시작 위치로 복사
				Array.Copy(_buffer.Array, _buffer.Offset + _readPos, _buffer.Array, _buffer.Offset, dataSize);
				_readPos = 0;
				_writePos = dataSize;
			}
		}

		/// <summary>
		/// "~까지 읽었어요" or "패킷 데이터로 바꿨어요"
		/// </summary>
		/// <param name="numOfBytes"></param>
		/// <returns></returns>
		public bool OnRead(int numOfBytes)
		{
			// 데이터를 읽은 후 => 읽기 커서를 옮김
			if (numOfBytes > DataSize)
				return false;

			_readPos += numOfBytes;
			return true;
		}

		/// <summary>
		/// "수신된 버퍼 데이터를 담겠어요"
		/// </summary>
		/// <param name="numOfBytes"></param>
		/// <returns></returns>
		public bool OnWrite(int numOfBytes)
		{
			// 데이터를 쓰기한 후 => 쓰기 커서를 옮김
			if (numOfBytes > FreeSize)
				return false;

			_writePos += numOfBytes;
			return true;
		}
	}
}
