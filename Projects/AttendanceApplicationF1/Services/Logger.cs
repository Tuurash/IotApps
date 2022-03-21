using System;
using System.IO;
using System.Text;

namespace AttendanceApplicationF1.Services
{
    public class LogFile
    {
        public enum LogType
        {
            /// <summary>simple text-log</summary>
            TXT,
            /// <summary>xhtml-formatted log</summary>
            XHTML_Plain
        };


        /// Supported log-Levels
        [Flags]
        public enum LogLevel
        {
            /// <summary>debug-level</summary>
            Debug = 0x01,
            /// <summary>info-level</summary>
            Info = 0x02,
            /// <summary>warn-level</summary>
            Warn = 0x04,
            /// <summary>errorlevel</summary>
            Error = 0x08,
            /// <summary>user-defined level 1</summary>
            User1 = 0x10,
            /// <summary>user-defined level 2</summary>
            User2 = 0x20,
            /// <summary>all levels</summary>
            All = 0xFF
        };

        /** PRIVATE GLOBAL VARS *****/
        private string Filename;
        private LogType Type;
        private LogLevel Level;
        private LogLevel DefaultLevel;

        ///<summary>Constructor with filename (no append)</summary> 
        ///<param name="filename">Filename</param>
        ///<exception cref="System.IO.IOException"> ;
        ///IOException - some trouble with wrting header to log-file
        ///</exception>
        public LogFile(string filename)
            : this(filename, false, LogType.TXT, LogLevel.All, "")
        { }

        public LogFile(string filename, bool append)
            : this(filename, append, LogType.TXT, LogLevel.All, "")
        { }

        ///<summary>Constructor with filename, append-info and log-type</summary> 
        ///<param name="filename">Filename</param>
        ///<param name="append">Append to existing file (true/false)</param>
        ///<param name="type">Log-Type</param>
        ///<exception cref="System.IO.IOException"> ;
        ///IOException - some trouble with wrting header to log-file (if append=false)
        ///</exception>
        public LogFile(string filename, bool append, LogType type)
            : this(filename, append, type, LogLevel.All, "")
        { }

        ///<summary>Constructor with filename, append-info and log-type</summary> 
        ///<param name="filename">Filename</param>
        ///<param name="append">Append to existing file (true/false)</param>
        ///<param name="type">Log-Type</param>
        ///<param name="level">Log-Level</param>
        ///<exception cref="System.IO.IOException"> ;
        ///IOException - some trouble with wrting header to log-file (if append=false)
        ///</exception>
        public LogFile(string filename, bool append, LogType type, LogLevel level)
            : this(filename, append, type, level, "")
        { }

        ///<summary>Constructor with filename, append-info, log-type, log-level and log-title</summary> 
        ///<param name="filename">Filename</param>
        ///<param name="append">Append to existing file (true/false)</param>
        ///<param name="type">Log-Type</param>
        ///<param name="level">Log-Level</param>
        ///<param name="title">Log-Title (first line in TXT or title-tag in HTML</param>
        ///<exception cref="System.IO.IOException"> ;
        ///IOException - some trouble with wrting header to log-file (if append=false)
        ///</exception>
        public LogFile(string filename, bool append, LogType type, LogLevel level, string title)
        {
            // set global vars
            this.Filename = filename;
            this.Type = type;
            this.Level = level;
            this.DefaultLevel = LogLevel.Debug;

            // write header
            if (!append)
            {
                // clear file
                WriteLine("", false);

                // if type=HTML write header
                if (Type == LogType.XHTML_Plain)
                {
                    LogRaw("<?xml version='1.0' ?>");
                    LogRaw("<!DOCTYPE html PUBLIC '-//W3C//DTD XHTML 1.0 Transitional//EN' " +
                        "'http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd'>");
                    LogRaw("<html xmlns='http://www.w3.org/1999/xhtml'>");
                    LogRaw("<head>");
                    if (title == "")
                        LogRaw("<title>Logfile " + DateTime.Today.ToShortDateString() + "</title>");
                    else
                        LogRaw("<title>" + title + "</title>");
                    LogRaw("<style type='text/css'>body{font-family:monospace;}</style>");
                    LogRaw("</head><body>");
                }
                // if type=TXT write title
                else if (Type == LogType.TXT)
                {
                    if (title != "") LogRaw(title);
                }
            };
        }

        public void WriteFooter()
        {
            // if type is HTML end it
            if (this.Type == LogType.XHTML_Plain)
                LogRaw("</body></html>");
        }


        public void LogRaw(string text)
        {
            WriteLine(text, true);
        }


        public void Log(string text)
        {
            Log(text, DefaultLevel);
        }

        public void Log(string text, LogLevel level)
        {
            // Check Level
            if ((level & this.Level) == 0) return;
            if (level == LogLevel.All) level = this.DefaultLogLevel;

            // format pre-string
            string prestring;
            switch (level)
            {
                case (LogLevel.Debug): prestring = "DEBUG " + DateTime.Now.ToString() + " "; break;
                case (LogLevel.Info): prestring = "INFO  " + DateTime.Now.ToString() + " "; break;
                case (LogLevel.Warn): prestring = "WARN  " + DateTime.Now.ToString() + " "; break;
                case (LogLevel.Error): prestring = "ERROR " + DateTime.Now.ToString() + " "; break;
                case (LogLevel.User1): prestring = "USER1 " + DateTime.Now.ToString() + " "; break;
                case (LogLevel.User2): prestring = "USER2 " + DateTime.Now.ToString() + " "; break;
                default: prestring = ""; break;
            }

            // format text depening on type
            string formatted_text;
            switch (this.Type)
            {
                // HTML_Plain
                case LogType.XHTML_Plain: formatted_text = prestring + text + "<br/>"; break;

                // PLAINTEXT
                default: formatted_text = prestring + text; break;
            }

            // write to file and flush
            this.WriteLine(formatted_text, true);
        }


        public LogType CurrentLogType
        {
            get
            {
                return Type;
            }
        }


        public LogLevel CurrentLogLevel
        {
            get
            {
                return Level;
            }
            set
            {
                Level = value;
            }
        }

        public LogLevel DefaultLogLevel
        {
            get
            {
                return DefaultLevel;
            }
            set
            {
                DefaultLevel = value;
            }
        }


        public Version Version
        {
            get
            {
                return (System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);
            }
        }

        /*** PRIVATE FUNCTIONS ***********************************************/

        // write line to file
        private void WriteLine(string text, bool append)
        {
            // open file
            // If an error occurs throw it to the caller.
            try
            {
                StreamWriter Writer = new StreamWriter(Filename, append, Encoding.UTF8);
                if (text != "") Writer.WriteLine(text);
                Writer.Flush();
                Writer.Close();
            }
            catch
            {
                throw;
            }
        }
    }
}
