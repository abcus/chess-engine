using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chess_Engine {
	public static class TimeControl {

		public static DateTime getFinishTime(DateTime startTime) {
			DateTime finishTime = startTime.AddMilliseconds(20000000);
			return finishTime;
		}
	}
}
