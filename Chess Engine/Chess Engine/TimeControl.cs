using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chess_Engine {
	public static class TimeControl {

		public static DateTime getFinishTime(SearchInfo info) {
			DateTime finishTime = new DateTime();

			if (info.moveTime != -1) {
				int moveTime = info.moveTime - 50;
				Console.WriteLine(moveTime);
				finishTime = DateTime.Now.AddMilliseconds(moveTime);
				return finishTime;
			}
			if (info.timeLeft != -1) {
				int moveTime = Math.Max((info.timeLeft / info.movesToGo - 50), 0);
				int adjustmentFactor = 2 - (Math.Min(10, UCI_IO.plyOutOfBook)) / 10;
				moveTime *= adjustmentFactor;

				if (info.increment != 0) {
					moveTime += info.increment;
				}
				Console.WriteLine(moveTime);
				finishTime = DateTime.Now.AddMilliseconds(moveTime);
				return finishTime;
			}
			return finishTime;
		}
	}
}
