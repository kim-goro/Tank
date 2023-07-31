using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Game
{
	/// <summary>
	/// 일감들을 던져받고 Queue 차곡차곡 쌓아줌
	/// 이를 flush해주는 쪽이 담당 스레드가 될 것임
	/// </summary>
	public class JobSerializer
	{
		JobTimer _timer = new JobTimer(); // 실행 시간이 명시된 Job을 담음
		Queue<IJob> _jobQueue = new Queue<IJob>(); // Job을 담음
		object _lock = new object(); // 일감의 순서만 보장하기 위한 역할
		bool _flush = false; // (=commit)

		// Push()되는 함수(Action들은 리턴값이 void이므로 반환값을 기대할 수 없음 => 언제 실행될지 모르는 비동기이기 때문에
		public IJob PushAfter(int tickAfter, Action action) { return PushAfter(tickAfter, new Job(action)); }
		public IJob PushAfter<T1>(int tickAfter, Action<T1> action, T1 t1) { return PushAfter(tickAfter, new Job<T1>(action, t1)); }
		public IJob PushAfter<T1, T2>(int tickAfter, Action<T1, T2> action, T1 t1, T2 t2) { return PushAfter(tickAfter, new Job<T1, T2>(action, t1, t2)); }
		public IJob PushAfter<T1, T2, T3>(int tickAfter, Action<T1, T2, T3> action, T1 t1, T2 t2, T3 t3) { return PushAfter(tickAfter, new Job<T1, T2, T3>(action, t1, t2, t3)); }

		public IJob PushAfter(int tickAfter, IJob job)
		{
			_timer.Push(job, tickAfter);
			return job;
		}

		public void Push(Action action) { Push(new Job(action)); }
		public void Push<T1>(Action<T1> action, T1 t1) { Push(new Job<T1>(action, t1)); }
		public void Push<T1, T2>(Action<T1, T2> action, T1 t1, T2 t2) { Push(new Job<T1, T2>(action, t1, t2)); }
		public void Push<T1, T2, T3>(Action<T1, T2, T3> action, T1 t1, T2 t2, T3 t3) { Push(new Job<T1, T2, T3>(action, t1, t2, t3)); }

		public void Push(IJob job)
		{
			lock (_lock)
			{
				_jobQueue.Enqueue(job);
			}
		}

		public void Flush()
		{
			_timer.Flush();

			while (true)
			{
				IJob job = Pop();
				if (job == null)
					return;

				job.Execute();
			}
		}

		IJob Pop()
		{
			lock (_lock)
			{
				if (_jobQueue.Count == 0)
				{
					_flush = false;
					return null;
				}
				return _jobQueue.Dequeue();
			}
		}
	}
}
