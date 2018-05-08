using System.Linq;
using System.Text;

namespace CMake
{
    public abstract class Instruction
    {
        public static string IntentString = new string(' ', 2);

        public static int CurrentIntent { get; set; }
        public static string CurrentIntentString => string.Concat(Enumerable.Repeat(IntentString, CurrentIntent));

        public virtual int Intent
        {
            get { return CurrentIntent; }
            set { CurrentIntent = value; }
        }

        public virtual string Comment { get; set; }
        public abstract void Write(StringBuilder sb);

        public override string ToString()
        {
            var sb = new StringBuilder();
            Write(sb);
            return sb.ToString();
        }
    }
}
