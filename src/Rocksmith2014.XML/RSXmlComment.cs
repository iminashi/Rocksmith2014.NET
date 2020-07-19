using Rocksmith2014.XML.Extensions;

namespace Rocksmith2014.XML
{
    /// <summary>
    /// Defines the type of an XML comment.
    /// </summary>
    public enum CommentType
    {
        Uninitialized,

        /// <summary>
        /// Song Creator Toolkit for Rocksmith version.
        /// </summary>
        Toolkit,

        /// <summary>
        /// Editor on Fire version.
        /// </summary>
        EOF,

        /// <summary>
        /// Dynamic Difficulty Creator version and used configuration.
        /// </summary>
        DDC,

        /// <summary>
        /// DDC Improver version.
        /// </summary>
        DDCImprover,

        /// <summary>
        /// Unknown comment type.
        /// </summary>
        Unknown
    }

    /// <summary>
    /// Represents an XML comment in a Rocksmith 2014 instrumental arrangement file.
    /// </summary>
    public sealed class RSXmlComment
    {
        private CommentType _commentType;

        /// <summary>
        /// Gets the value of the comment.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Gets the type of the comment.
        /// </summary>
        public CommentType CommentType
        {
            get
            {
                if (!string.IsNullOrEmpty(Value) && _commentType == CommentType.Uninitialized)
                {
                    if (Value.IgnoreCaseContains("CST v"))
                        _commentType = CommentType.Toolkit;
                    else if (Value.IgnoreCaseContains("EOF"))
                        _commentType = CommentType.EOF;
                    else if (Value.IgnoreCaseContains("DDC Improver"))
                        _commentType = CommentType.DDCImprover;
                    else if (Value.IgnoreCaseContains("DDC v"))
                        _commentType = CommentType.DDC;
                    else
                        _commentType = CommentType.Unknown;
                }

                return _commentType;
            }
        }

        /// <summary>
        /// Creates a new Rocksmith XML comment.
        /// </summary>
        /// <param name="comment"></param>
        public RSXmlComment(string comment) => Value = comment;

        public override string ToString() => Value;
    }
}
