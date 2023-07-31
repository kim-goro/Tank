using System;
using System.Collections.Generic;
using System.Text;
using ServerCore;

namespace Server.Game
{
	/// <summary>
	/// (실행 시간을 포함한) 일감 단위 설계
	/// </summary>
	struct JobTimerElem : IComparable<JobTimerElem>
	{
		public int execTick; // 실행 시간
		public IJob job;

		/// <summary>
		/// 실행 시간(중요도) 비교
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public int CompareTo(JobTimerElem other)
		{
			return other.execTick - execTick;
		}
	}

	/// <summary>
	/// 일감들을 (임박시간에 맞춰) 순차적으로 실행시켜줌
	/// </summary>
	public class JobTimer
	{
		PriorityQueue<JobTimerElem> _pq = new PriorityQueue<JobTimerElem>();
		object _lock = new object();

		public void Push(IJob job, int tickAfter = 0) // "몇 틱 후에 실행하길 원하냐?"
		{
			JobTimerElem jobElement;
			jobElement.execTick = System.Environment.TickCount + tickAfter;
			jobElement.job = job;

			lock (_lock)
			{
				_pq.Push(jobElement);
			}
		}

		public void Flush()
		{
			while (true)
			{
				int now = System.Environment.TickCount; // 컴퓨터가 마지막으로 시작된 이후 경과된 시간(밀리초)

				JobTimerElem jobElement;

				lock (_lock)
				{
					if (_pq.Count == 0)
						break;

					jobElement = _pq.Peek();
					if (jobElement.execTick > now)
						break;

					_pq.Pop(); // 때가 된 애들은 모두 실행시켜줌
				}

				jobElement.job.Execute();
			}
		}
	}
}
