using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CMake.Instructions
{
    public class Message : GenericInstruction {
        
        public enum Mode
        {
            None, 
            Status,
            Warning,
            Author_Warning,
            Send_Error,
            Fatal_Error,
            Deprication,
        }

        public static Message Create(string message, Mode mode = Mode.None)
        {
            return new Message()
            {
                message = message,
                mode = mode
            };
        }
        
        public Mode mode { get; set; }
        public string message { get; set; }
        
        
        public override string Command 
        {
            get { return $"message ({(mode == Mode.None ? "" : mode.ToString().ToUpper())} \"{message}\")"; }
            set { }
        }

        //We probably don't need to comment on a log message...
        public override string Comment
        {
            get { return null; }
            set { }
        }

    }
}
