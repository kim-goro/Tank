using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ServerCore
{
	/// <summary>
	/// 패킷을 Send()하고자 할때, 시리얼라이즈한 버퍼 데이터를 하나의 큰 데이터에 순차적으로 쌓아줌 
	/// </summary>
	public class SendBufferHelper
	{
		public static ThreadLocal<SendBuffer> CurrentBuffer = new ThreadLocal<SendBuffer>(() => { return null; }); // 각 스레드(클라이언트 세션) 마다 Send()할 버퍼 데이터를 모아둔 공간(TLS)
		public static int ChunkSize { get; set; } = 65535 * 100; // 보낼 수 있는 최대 byte수

		/// <summary>
		/// "데이터를 담겠다"
		/// </summary>
		/// <param name="reserveSize">예약 사이즈(=담을 데이터 사이즈)</param>
		/// <returns></returns>
		public static ArraySegment<byte> Open(int reserveSize)
		{
			// 첫 메모리 생성
			if (CurrentBuffer.Value == null)
				CurrentBuffer.Value = new SendBuffer(ChunkSize); 

			if (CurrentBuffer.Value.FreeSize < reserveSize) // 남은 공간(free)보다 예약(reserve) 데이터가 큰 경우
				CurrentBuffer.Value = new SendBuffer(ChunkSize); // 메모리 재생성

			return CurrentBuffer.Value.Open(reserveSize);
		}

		/// <summary>
		/// "데이터를 다 담았으니 Cursor 옮겨주세요"
		/// </summary>
		/// <param name="usedSize">담은 데이터의 양</param>
		/// <returns></returns>
		public static ArraySegment<byte> Close(int usedSize)
		{
			return CurrentBuffer.Value.Close(usedSize);
		}
	}

	public class SendBuffer
	{
		byte[] _buffer; // 메모리 사이즈
		int _usedSize = 0; // 쓰기 Cursor위치 "내가 얼마만큼 사용했나?"

		public int FreeSize { get { return _buffer.Length - _usedSize; } } // 남은 쓰기 여유공간

		public SendBuffer(int chunkSize)
		{
			_buffer = new byte[chunkSize];
		}

		/// <summary>
		/// " 00만큼의 데이터를 쓰겠어"
		/// </summary>
		/// <param name="reserveSize">얼마만큼의 데이터를 쓰기할건지</param>
		/// <returns></returns>
		public ArraySegment<byte> Open(int reserveSize)
		{
			if (reserveSize > FreeSize) // 예약(reserve)공간보다 남은 공간이 크면 null을 리턴
				return null;

			return new ArraySegment<byte>(_buffer, _usedSize, reserveSize); // 작업할 영역을 동강 잘라서 줌
		}

		/// <summary>
		/// "00만큼의 데이터를 썼어"
		/// </summary>
		/// <param name="usedSize">데이터 쓰기 확정</param>
		/// <returns></returns>
		public ArraySegment<byte> Close(int usedSize)
		{
			ArraySegment<byte> segment = new ArraySegment<byte>(_buffer, _usedSize, usedSize);
			_usedSize += usedSize;
			return segment; // 다시 유효범위를 리턴해줌
		}
	}
}