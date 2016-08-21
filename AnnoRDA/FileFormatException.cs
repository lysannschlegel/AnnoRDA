using System;

namespace AnnoRDA
{
    public class FileFormatException : FormatException
    {
        public enum EntityType
        {
            RDAHeader,
            BlockHeader,
            FileHeader,
        }

        public enum Error
        {
            UnexpectedEndOfFile,
            InvalidValue,
        }

        public EntityType Entity { get; }
        public Error EntityError { get; }
        public long? Offset { get; }
        public string DetailMessage { get; }

        public FileFormatException(EntityType entity, Error error, long? offset = null, string detailMessage = null, Exception innerException = null)
        : base("Input file or data stream does not conform to the expected file format specification: ", innerException)
        {
            this.Entity = entity;
            this.EntityError = error;
            this.Offset = offset;
            this.DetailMessage = detailMessage;
        }


        public override string Message
        {
            get
            {
                string error = GetErrorString(this.EntityError);
                
                string entity = GetEntityString(this.Entity);
                if (!String.IsNullOrEmpty(entity)) {
                    error += " in " + entity;
                }

                if (this.Offset.HasValue) {
                    error += " at offset " + this.Offset.Value;
                }

                error += ".";

                if (!String.IsNullOrEmpty(this.DetailMessage)) {
                    error += " " + this.DetailMessage;
                }

                return base.Message + " " + error;
            }
        }

        private static string GetEntityString(EntityType entity)
        {
            switch (entity) {
                case EntityType.RDAHeader: return "header";
                case EntityType.BlockHeader: return "block header";
                case EntityType.FileHeader: return "file header";
            }
            return null;
        }

        private static string GetErrorString(Error error)
        {
            switch (error) {
                case Error.UnexpectedEndOfFile: return "Unexpected end of file";
                case Error.InvalidValue: return "Invalid value";
            }
            return "Error";
        }


        public override bool Equals(object obj)
        {
            if (!(obj is FileFormatException)) {
                return false;
            }
            FileFormatException ex = (FileFormatException)obj;

            if (ex.Entity != this.Entity) {
                return false;
            }
            if (ex.EntityError != this.EntityError) {
                return false;
            }
            if (ex.Offset != this.Offset) {
                return false;
            }
            if (ex.DetailMessage != this.DetailMessage) {
                return false;
            }
            if ((ex.InnerException == null) != (this.InnerException == null)) {
                return false;
            }
            if (ex.InnerException != null) {
                if (ex.InnerException.GetType() != this.InnerException.GetType()) {
                    return false;
                }
            }
            return true;
        }

        public override int GetHashCode()
        {
            int result = this.Entity.GetHashCode() * 23;
            result += this.EntityError.GetHashCode() * 11;
            result += this.Offset.GetHashCode() * 7;
            result += this.DetailMessage.GetHashCode() * 3;
            if (this.InnerException != null) {
                result += this.InnerException.GetType().GetHashCode();
            }
            return result;
        }
    }
}
