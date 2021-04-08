using System.Net;
using NLog;

namespace LuaInterface.Editor
{
    public static class Logger
    {
        // common
        static DefaultLogMessageFormatter defaultFormatter;
        static TimestampFormatter timestampFormatter;
        static ColorCodeFormatter colorFormatter;

        // file 
        static string filePath = "Logs/Log.txt";
        static FileWriter fileWriter;

        // socket 
        static string sendToIpAddress = "127.0.0.1";
        static int sendOnPort = 1234;
        static SocketAppender socket;

        public static void Initialize()
        {
            // common
            defaultFormatter = new DefaultLogMessageFormatter();
            timestampFormatter = new TimestampFormatter();
            colorFormatter = new ColorCodeFormatter();

            // file
            fileWriter = new FileWriter(filePath);

            LoggerFactory.AddAppender(write);
            
            // socket
            socket = new SocketAppender();

            LoggerFactory.AddAppender(send);

            socket.Connect(IPAddress.Parse(sendToIpAddress), sendOnPort);
        }

        static void write(NLog.Logger logger, LogLevel logLevel, string message)
        {
            message = defaultFormatter.FormatMessage(logger, logLevel, message);
            message = timestampFormatter.FormatMessage(logger, logLevel, message);
            fileWriter.WriteLine(message);
        }

        static void send(NLog.Logger logger, LogLevel logLevel, string message) {
            message = defaultFormatter.FormatMessage(logger, logLevel, message);
            message = timestampFormatter.FormatMessage(logger, logLevel, message);
            message = colorFormatter.FormatMessage(logLevel, message);
            socket.Send(logLevel, message);
        }

        public static void Terminate()
        {
            // socket
            socket.Disconnect();

            LoggerFactory.RemoveAppender(send);
 
            // file
            LoggerFactory.RemoveAppender(write);
        }
    }
}