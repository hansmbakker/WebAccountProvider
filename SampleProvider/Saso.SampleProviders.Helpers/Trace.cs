using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Saso.SampleProviders.Helpers
{
    public enum TraceLevel
    {
        Verbose = 1 , 
        Warn    = 1 << 2 , 
        Error  =  1 << 3 
    };

    public class Trace
    {

        static Action<string> traceListener; 

        static Trace ()
        {
            Level = TraceLevel.Verbose; 
        }
        public static TraceLevel Level { get; set;  }
      
        public static void AddListener( Action<string> listener )
        {
            traceListener = listener; 

        }
        public static void Log (  string value )
        {
            Log(value, TraceLevel.Verbose ); 
        }

        public static void Warn ( string value  )
        {
            Log(value, TraceLevel.Warn); 
        }

        public static void Error (string value)
        {
            Log(value, TraceLevel.Error );
        }
        
        public static void LogException ( Exception e , TraceLevel level = TraceLevel.Error )
        {
            Log(e.Message, level );
            Log(e.StackTrace, level); 
        }

        private static void Log ( string message, TraceLevel level )
        {
            //TODO: Need to implement for real. Right now we just Debug Trace 
            if ( (Level & level) != 0 )
                System.Diagnostics.Debug.WriteLine($"{level.ToString().ToUpper()}: {message}");

            if (traceListener != null)
                traceListener(message); 
        }

       

    }
}
