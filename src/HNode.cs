#region Copyright (c) 2017 Atif Aziz
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
#endregion

namespace High5
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using MoreLinq;

    public static class HNodeFactory
    {
        public static HAttribute Attribute(string name, string value) =>
            new HAttribute(name, value);

        public static IEnumerable<HAttribute> Attributes(params (string Name, string Value)[] attributes) =>
            from a in attributes select Attribute(a.Name, a.Value);

        public static HElement Html(params HNode[] children) => Element("html", children);
        public static HElement Title(string text) => Element("title", Text(text));
        public static HElement Title(IEnumerable<HAttribute> attributes, string text) => Element("title", Text(text));
        public static HElement Link(params HAttribute[] attributes) => Element("link", attributes);
        public static HElement Script(IEnumerable<HAttribute> attributes, params HNode[] children) => Element("script", attributes, children);

        public static HDocumentFragment DocumentFragment(params HNode[] children) =>
            DocumentFragment((IEnumerable<HNode>) children);

        public static HDocumentFragment DocumentFragment(IEnumerable<HNode> children) =>
            new HDocumentFragment(children);

        public static HElement Element(string tagName) =>
            Element(tagName, new HNode[] {});

        public static HElement Element(string tagName, params HNode[] children) =>
            Element(tagName, null, children);

        public static HElement Element(string tagName, params HAttribute[] attributes) =>
            Element(tagName, attributes, null);

        public static HElement Element(string tagName, IEnumerable<HAttribute> attributes, params HNode[] children)
        {
            return new HElement(tagName, null, attributes, Children());

            IEnumerable<HNode> Children()
            {
                foreach (var child in children)
                {
                    if (child is HDocumentFragment documentFragment)
                    {
                        foreach (var grandChild in documentFragment.ChildNodes)
                            yield return grandChild;
                    }
                    else
                    {
                        yield return child;
                    }
                }
            }
        }

        public static IEnumerable<HNode> MergeText(params HNode[] nodes) =>
            MergeText((IEnumerable<HNode>) nodes);

        public static IEnumerable<HNode> MergeText(IEnumerable<HNode> nodes) =>
            from g in nodes.GroupAdjacent(node => node is HText, (k, g) => new KeyValuePair<bool, IEnumerable<HNode>>(k, g))
            select g.Key
                 ? from text in new[] { Text(string.Concat(from text in g.Value.Cast<HText>() select text.Value)) }
                   where text.Value.Length > 0
                   select text
                 : g.Value
            into ns
            from node in ns
            select node;

        static IEnumerable<(bool IsText, IEnumerable<HNode> Nodes)> GroupText(IEnumerable<HNode> nodes)
        {
            // Implementation adapted from MoreLINQ's GroupAdjacent:
            // https://morelinq.github.io/1.x/ref/api/html/Overload_MoreLinq_MoreEnumerable_GroupAdjacent.htm

            #region Copyright (c) 2012 Atif Aziz. All rights reserved.
            //
            // Licensed under the Apache License, Version 2.0 (the "License");
            // you may not use this file except in compliance with the License.
            // You may obtain a copy of the License at
            //
            //     http://www.apache.org/licenses/LICENSE-2.0
            //
            // Unless required by applicable law or agreed to in writing, software
            // distributed under the License is distributed on an "AS IS" BASIS,
            // WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
            // See the License for the specific language governing permissions and
            // limitations under the License.
            //
            #endregion

            using (var iterator = nodes.GetEnumerator())
            {
                var group = default(bool);
                var members = (List<HNode>) null;

                while (iterator.MoveNext())
                {
                    var text = iterator.Current is HText t ? t : null;
                    var key = text != null;
                    var element = text ?? iterator.Current;

                    if (members != null && group == key)
                    {
                        members.Add(element);
                    }
                    else
                    {
                        if (members != null)
                            yield return (group, members);
                        group = key;
                        members = new List<HNode> { element };
                    }
                }

                if (members != null)
                    yield return (group, members);
            }
        }
        static IEnumerable<HNode> Content0(IEnumerable<HNode> children)
        {
            var sb = default(StringBuilder);
            var textPending = false;

            HText PopPendingText()
            {
                if (!textPending)
                    return null;
                sb.Length = 0;
                textPending = false;
                return Text(sb.ToString());
            }

            foreach (var child in children)
            {
                if (child is HDocumentFragment documentFragment)
                {
                    if (PopPendingText() is HText pendingText)
                        yield return pendingText;

                    foreach (var grandChild in documentFragment.ChildNodes)
                        yield return grandChild;
                }
                else if (child is HText text)
                {
                    if (text.Value.Length > 0)
                    {
                        if (sb == null)
                            sb = new StringBuilder();
                        sb.Append(text.Value);
                        textPending = true;
                    }
                }
                else
                {
                    if (PopPendingText() is HText pendingText)
                        yield return pendingText;

                    yield return child;
                }
            }

            if (PopPendingText() is HText finalPendingText)
                yield return finalPendingText;
            /*
            var stack = new Stack<IEnumerator<Node>>();
            try
            {
                stack.Push(children.GetEnumerator());

                while (stack.Count > 0)
                {
                    var e = stack.Pop();
                    while (e.MoveNext())
                    {
                        var node = e.Current;
                        if (node is DocumentFragment documentFragment)
                        {
                            stack.Push(e);
                            stack.Push(documentFragment.ChildNodes.GetEnumerator());

                        }
                        else
                        {
                            yield return node;
                        }
                    }
                }
            }
            finally
            {
                foreach (var e in stack)
                    e.Dispose();
            }
            */
        }

        public static HText Text(string value) =>
            new HText(value);
    }

    public abstract class HNode
    {
        static readonly HNode[] ZeroNodes = new HNode[0];

        public IEnumerable<HNode> ChildNodes { get; }

        protected HNode(IEnumerable<HNode> children) =>
            ChildNodes = children ?? ZeroNodes;
    }

    public sealed class HAttribute
    {
        public string Name { get; }
        public string Value { get; }

        public HAttribute(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }

    public sealed class HElement : HNode
    {
        public static readonly HAttribute[] ZeroAttributes = new HAttribute[0];

        public string TagName { get; }
        public string NamespaceUri { get; }
        public IEnumerable<HAttribute> Attributes { get; }

        public HElement(string tagName,
                       IEnumerable<HAttribute> attributes,
                       IEnumerable<HNode> children) :
            this(tagName, null, attributes, children) {}

        public HElement(string tagName, string namespaceUri,
                       IEnumerable<HAttribute> attributes,
                       IEnumerable<HNode> children) :
            base(children)
        {
            TagName = tagName;
            NamespaceUri = namespaceUri;
            Attributes = attributes ?? ZeroAttributes;
        }
    }

    public sealed class HText : HNode
    {
        public string Value { get; }

        public HText(string value) :
            base(Enumerable.Empty<HNode>()) => Value = value;
    }

    public sealed class HComment : HNode
    {
        public string Data { get; }

        public HComment(string data) :
            base(Enumerable.Empty<HNode>()) => Data = data;
    }

    public sealed class HDocument : HNode
    {
        public HDocument(IEnumerable<HNode> children) : base(children) {}
    }

    public sealed class HDocumentFragment : HNode
    {
        public HDocumentFragment(IEnumerable<HNode> children) : base(children) {}
    }

    /*
    public sealed class HtmlNodeTests
    {
        [Fact]
        public void ChildNodesIsNotNull()
        {
            var node = new HtmlNode();
            Assert.NotNull(node.ChildNodes);
        }

        [Fact]
        public void DefaultNodeHasNoChildren()
        {
            var node = new HtmlNode();
            Assert.Equal(0, node.ChildNodes.Count);
            Assert.Null(node.ParentNode);
        }

        [Fact]
        public void DefaultNodeHasNoParent()
        {
            var node = new HtmlNode();
            Assert.Null(node.ParentNode);
        }

        [Fact]
        public void ChildrenAreAddedInOrder()
        {
            /*
            var node = new HtmlNode();
            var children = new[] { new HtmlNode(), new HtmlNode(), new HtmlNode() };
            foreach (var child in children)
                node.ChildNodes.Add(child);
            Assert.Equal(3, node.ChildNodes.Count);
            Assert.Equal(children.Cast<High5.HtmlNode>(), node.ChildNodes);
            */
            /*
            var bar = Element("bar");
            var baz = Element("bar");
            var qux = Text("qux");
            var foo = Element("foo", bar, baz, qux);

            Assert.False(foo.Attributes.Any());
            Assert.Equal(3, foo.ChildNodes.Count());
            Assert.Equal(new Node[] { bar, baz, qux }, foo.ChildNodes);

            var samplePage =
                Element("html",
                    Element("head",
                        Title("Little HTML DSL"),
                        Link(Attribute("rel", "https://instabt.com/instaBT.ico")),
                        Script(Attributes(("type", "text/javascript"), ("src", "js/jquery-2.1.0.min.js"))),
                        Script(Attributes(("type", "text/javascript")),
                            Text("$().ready(function () { setup(); });"))),
                    Element("body",
                        Element("div", Attributes(("id", "content")),
                            Element("p", Text("Hello world.")),
                            Element("br"),
                            Element("img", Attribute("src", "http://fsharp.org/img/logo/fsharp256.png")))));
        }

        sealed class HtmlNode : High5.HtmlNode {}
    }
    */
}
