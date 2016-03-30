using System;
using Toggl.Phoebe.Data.Models;

namespace Toggl.Phoebe.Data
{
    public struct Duration {

        private readonly int encoded;
        private readonly int digits;

        public Duration (int hours, int minutes) : this (hours * 100 + minutes)
        {
        }

        private Duration (int encoded)
        {
            this.encoded = encoded;
            this.digits = GetDigits (encoded);
        }

        public Duration RemoveDigit ()
        {
            return new Duration (encoded / 10);
        }

        public Duration AppendDigit (int digit)
        {
            return new Duration (encoded * 10 + digit);
        }

        public Duration AddMinutes (int minutes)
        {
            minutes += Minutes;
            var hours = Hours + minutes / 60;
            minutes = minutes % 60;
            return new Duration (hours, minutes);
        }

        public override bool Equals (object obj)
        {
            return obj is Duration && this == (Duration)obj;
        }

        public override int GetHashCode ()
        {
            return encoded.GetHashCode ();
        }

        public override string ToString ()
        {
            return String.Format ("{0:00}:{1:00}", Hours, Minutes);
        }

        public bool IsValid
        {
            get { return Minutes < 60 && Digits < 5; }
        }

        public int Digits
        {
            get { return digits; }
        }

        public int Hours
        {
            get { return encoded / 100; }
        }

        public int Minutes
        {
            get { return encoded % 100; }
        }

        private static int GetDigits (int encoded)
        {
            int digits = 0;
            while (encoded > 0) {
                digits += 1;
                encoded /= 10;
            }
            return digits;
        }

        public static bool operator == (Duration left, Duration right)
        {
            return left.encoded == right.encoded;
        }

        public static bool operator != (Duration left, Duration right)
        {
            return ! (left == right);
        }

        public static implicit operator Duration (TimeSpan timespan)
        {
            return new Duration (timespan.Hours, timespan.Minutes);
        }

        public static Duration Zero
        {
            get { return new Duration (); }
        }
    }

    public enum DurationFormat {
        Classic,
        Improved,
        Decimal
    }
}
