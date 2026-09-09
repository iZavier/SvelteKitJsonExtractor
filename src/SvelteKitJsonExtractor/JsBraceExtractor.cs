namespace SvelteKitJsonExtractor
{
    // Pulls a balanced { ... } block out of a chunk of JS source, without
    // getting tripped up by braces sitting inside strings/comments.
    // Internal, but visible to the test project (see AssemblyInfo.cs) -
    // this is where the actual logic lives so it's worth testing directly.
    internal static class JsBraceExtractor
    {
        // Finds kit.start(, then grabs the object literal that's its 3rd
        // arg (the config/data blob). Returns null if there's no kit.start
        // call, or the braces don't balance.
        public static string ExtractKitStartArgument(string source)
        {
            if (source == null) return null;

            int callIndex = source.IndexOf("kit.start(", StringComparison.Ordinal);
            if (callIndex < 0) return null;

            var scanner = new JsScanner(source) { Position = callIndex + "kit.start(".Length };
            int parenDepth = 1; // already past the call's opening (

            while (scanner.Position < source.Length && parenDepth > 0)
            {
                if (scanner.TrySkipStringOrComment())
                    continue;

                char c = scanner.Current;

                if (c == '(') parenDepth++;
                else if (c == ')') parenDepth--;
                else if (c == '{' && parenDepth == 1)
                {
                    // first top-level { inside the call = start of the config object
                    return ExtractBalancedBraces(source, scanner.Position);
                }

                scanner.Advance();
            }

            return null;
        }

        internal static string ExtractBalancedBraces(string source, int start)
        {
            if (start < 0 || start >= source.Length || source[start] != '{')
                return null;

            var scanner = new JsScanner(source) { Position = start };
            int depth = 0;

            while (scanner.Position < source.Length)
            {
                if (scanner.TrySkipStringOrComment())
                    continue;

                char c = scanner.Current;
                if (c == '{') depth++;
                else if (c == '}')
                {
                    depth--;
                    if (depth == 0)
                        return source.Substring(start, scanner.Position - start + 1);
                }

                scanner.Advance();
            }

            return null; // shouldn't really happen on well-formed JS
        }

        // Tiny forward-only scanner that knows how to skip past strings
        // ('...', "...", `...`) and comments (// and /* */) so the brace
        // counting above doesn't get confused by braces living inside them.
        private class JsScanner
        {
            private readonly string _s;
            public int Position;

            public JsScanner(string s) => _s = s;

            public char Current => _s[Position];

            public void Advance() => Position++;

            public bool TrySkipStringOrComment()
            {
                char c = Current;

                if (c == '"' || c == '\'' || c == '`')
                {
                    char quote = c;
                    Position++;
                    while (Position < _s.Length)
                    {
                        if (_s[Position] == '\\') { Position += 2; continue; }
                        if (_s[Position] == quote) { Position++; break; }
                        Position++;
                    }
                    return true;
                }

                if (c == '/' && Position + 1 < _s.Length)
                {
                    if (_s[Position + 1] == '/')
                    {
                        Position += 2;
                        while (Position < _s.Length && _s[Position] != '\n') Position++;
                        return true;
                    }
                    if (_s[Position + 1] == '*')
                    {
                        Position += 2;
                        int end = _s.IndexOf("*/", Position, StringComparison.Ordinal);
                        Position = end < 0 ? _s.Length : end + 2;
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
