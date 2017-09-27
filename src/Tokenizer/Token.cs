namespace ParseFive.Tokenizer
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using static TokenType;

    class Token
    {
        public TokenType Type { get; }
        public string TagName { get; set; }
        public List<Attr> Attrs { get; set; }
        public bool SelfClosing { get; set; }

        public Token(TokenType type, string tagName, bool selfClosing, List<Attr> attrs) //START TAG
        {
            this.Type = type;
            this.TagName = tagName;
            this.SelfClosing = selfClosing;
            this.Attrs = attrs;
        }

        public Token(TokenType type, string tagName, List<Attr> attrs) //end tag
        {
            this.Type = type;
            this.TagName = tagName;
            this.Attrs = attrs;
        }

        public Token(TokenType type) //Hibernation && EOF
        {
            this.Type = type;
        }
    }

    sealed class DoctypeToken : Token
    {
        public string Name { get; set; }
        public bool ForceQuirks { get; set; }
        public string PublicId { get; set; }
        public string SystemId { get; set; }

        public DoctypeToken(string name,
                            bool forceQuirks = false,
                            string publicId = null,
                            string systemId = null) :
            base(DOCTYPE_TOKEN)
        {
            this.Name = name;
            this.ForceQuirks = forceQuirks;
            this.PublicId = publicId;
            this.SystemId = systemId;
        }
    }

    sealed class CommentToken : Token
    {
        public string Data { get; set; }

        public CommentToken(string data) :
            base(COMMENT_TOKEN)
        {
            this.Data = data;
        }
    }

    sealed class CharacterToken : Token
    {
        public string Chars { get; set; }

        public CharacterToken(TokenType type, char ch) :
            base(type)
        {
            Debug.Assert(type == CHARACTER_TOKEN
                      || type == WHITESPACE_CHARACTER_TOKEN
                      || type == NULL_CHARACTER_TOKEN);
            this.Chars = ch.ToString();
        }
    }
}