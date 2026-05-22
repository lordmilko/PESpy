using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace PESpy
{
    internal static unsafe class SymRegex
    {
        internal static bool IsMatch(ReadOnlySpan<char> str, ReadOnlySpan<char> regex, bool ignoreCase)
        {
            Debug.Assert(str[str.Length - 1] == '\0');
            Debug.Assert(regex[regex.Length - 1] == '\0');

            fixed (char* pStr = str)
            fixed (char* pRegex = regex)
            {
                //CompareRE returns the opposite of what you'd expect
                return !CompareRE(pStr, pRegex, !ignoreCase);
            }
        }

        //Incredibly, this is seems to be actually way faster than the wide version
        public static bool IsMatch(SymString str, string regex, bool ignoreCase) //I think string may be guaranteed to be null terminated, but ReadOnlySpan<char> is not
        {
            //In Debug builds this is too slow. It's a lot faster on Release

            fixed (char* pRegex = regex)
            {
                //CompareRE returns the opposite of what you'd expect
                return !CompareRE(str.Value, pRegex, !ignoreCase);
            }
        }

        private const char REGEX_ACTION_END = (char) 0;
        private const char REGEX_ACTION_QUESTION = (char) 1;
        private const char REGEX_ACTION_ASTERISK = (char) 2;
        private const char REGEX_ACTION_OPEN_BRACKET = (char) 3;
        private const char REGEX_ACTION_CLOSE_BRACKET = (char) 4;
        private const char REGEX_ACTION_PLUS = (char) 5;
        private const char REGEX_ACTION_HASH = (char) 6;

        #region Ansi

        //The input strings _must_ be null terminated. It's too slow trying to calculate the length of each string beforehand
        public static bool CompareRE(byte* pStr, char* pRE, bool fCase)
        {
            char* regexAtStartOfLoop = default; // r15
            char* regexFromPreviousLoop; // rdi
            byte action; // ebx

            while (true)
            {
                //On each iteration, the current regex shifts down to regexAtStartOfLoop,
                //and the previous value in regexAtStartOfLoop is moved into regexFromPreviousLoop
                regexFromPreviousLoop = regexAtStartOfLoop;
                regexAtStartOfLoop = pRE;

                action = (byte) nextToken(&pRE, fCase);

                switch (action)
                {
                    case (byte) REGEX_ACTION_END:
                        return *pStr != 0;
                    case (byte) REGEX_ACTION_QUESTION: //Match any character (one time)
                        if (*pStr != 0)
                        {
                            ++pStr;
                            continue;
                        }
                        return true;
                    case (byte) REGEX_ACTION_ASTERISK:
                        while (CompareRE(pStr, pRE, fCase))
                        {
                            if (*pStr++ == 0)
                                return true;
                        }
                        return false;

                    case (byte) REGEX_ACTION_OPEN_BRACKET:
                        {
                            //Match one of several possible values within the brackets.
                            //e.g. [az]bc will match abc or zbc. If you specify a hyphen,
                            //The value inside the brackets becomes a range. e.g. [a-d]bc matches
                            //abc, bbc, cbc and dbc

                            var ch = *pStr;

                            if (ch == 0)
                                return true;

                            if (!fCase)
                            {
                                if ((uint) (ch - (byte) 'A') <= ('Z' - 'A'))
                                    ch = (byte) (ch | 0x20);
                            }

                            var bracketedLowerBounds = (byte) '\0';

                            //e.g. if you have [ab] bracketedChar might be the "a", and we're now going to try and match the "a" with the current character in the string
                            var bracketedChar = (byte) nextToken(&pRE, fCase);

                            if (bracketedChar == REGEX_ACTION_END)
                                continue;

                            CheckForCloseBracket:
                            if (bracketedChar == REGEX_ACTION_CLOSE_BRACKET)
                            {
                                if (nextToken(&pRE, fCase) != REGEX_ACTION_HASH)
                                    return true;

                                MoveNext(bracketedChar, &pRE, fCase);
                                continue;
                            }
                            if (bracketedChar == '-') //If a '-' is specified, the value within the brackets is a range
                            {
                                var bracketedUpperBounds = (byte) nextToken(&pRE, fCase);
                                bracketedChar = bracketedUpperBounds;

                                if ((bracketedUpperBounds & 0xFFFB) == 0)
                                    return true;

                                if (ch >= bracketedLowerBounds && ch <= bracketedUpperBounds)
                                {
                                    ++pStr;

                                    MoveNext(bracketedChar, &pRE, fCase);
                                    continue;
                                }
                            }

                            bracketedLowerBounds = bracketedChar;

                            //If we have "bbc" and "[ab]bc", if the "a" doesn't match, get the next character (the "b") and try match that instead
                            if (ch != bracketedChar)
                            {
                                bracketedChar = (byte) nextToken(&pRE, fCase);

                                if (bracketedChar == (byte) REGEX_ACTION_END)
                                    continue;

                                goto CheckForCloseBracket;
                            }

                            ++pStr;

                            if (bracketedChar != (byte) REGEX_ACTION_END)
                                MoveNext(bracketedChar, &pRE, fCase);

                            continue;
                        }

                    case (byte) REGEX_ACTION_PLUS:
                    case (byte) REGEX_ACTION_HASH: //With optional prefix. i.e. a#b matches "b", "ab" but not "a", which is the optional prefix part
                        if (!CompareRE(pStr, pRE, fCase))
                            return false;

                        pRE = regexFromPreviousLoop;
                        continue;
                    default:
                    {
                        /* Whenever we encounter a wildcard, the wildcard case takes over all processing
                         * until we reach the end of the string. On the inner call to CompareRE, pStr is still
                         * on the previous character before the *. e.g. if we have abc / a*c, *pStr will be on
                         * the "b" while processing the wildcard. Because of this action will not match _ch below, and we'll
                         * return immediately. The case for handling wildcards will keep calling CompareRE for each successive
                         * character, until we hit the end of the string. If yet another wildcard is encountered, the nested
                         * wildcard case will then start doing the same thing as the parent, and the string processing will have
                         * been taken over yet again */
                        var ch = *pStr;

                        if (!fCase)
                        {
                            if (unchecked((uint) (ch - (byte) 'A') <= ('Z' - 'A')))
                                ch = (byte) (ch | 0x20);
                        }

                        if (action == ch)
                        {
                            ++pStr;
                            continue;
                        }
                        if (nextToken(&pRE, fCase) == (byte) REGEX_ACTION_HASH)
                            continue;

                        return true;
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void MoveNext(byte subAction, char** _pRE, bool fCase)
        {
            do
            {
                if (subAction == (byte) REGEX_ACTION_CLOSE_BRACKET)
                    break;

                subAction = (byte) nextToken(_pRE, fCase);
            }
            while (subAction != REGEX_ACTION_END);
        }

        #endregion
        #region Wide

        public static bool CompareRE(char* pStr, char* pRE, bool fCase)
        {
            char* regexAtStartOfLoop = default; // r15
            char* regexFromPreviousLoop; // rdi
            char action; // ebx

            while (true)
            {
                //On each iteration, the current regex shifts down to regexAtStartOfLoop,
                //and the previous value in regexAtStartOfLoop is moved into regexFromPreviousLoop
                regexFromPreviousLoop = regexAtStartOfLoop;
                regexAtStartOfLoop = pRE;

                action = nextToken(&pRE, fCase);

                switch (action)
                {
                    case REGEX_ACTION_END:
                        return *pStr != '\0';
                    case REGEX_ACTION_QUESTION: //Match any character (one time)
                        if (*pStr != '\0')
                        {
                            ++pStr;
                            continue;
                        }
                        return true;
                    case REGEX_ACTION_ASTERISK:
                        while (CompareRE(pStr, pRE, fCase))
                        {
                            if (*pStr++ == '\0')
                                return true;
                        }
                        return false;

                    case REGEX_ACTION_OPEN_BRACKET:
                    {
                        //Match one of several possible values within the brackets.
                        //e.g. [az]bc will match abc or zbc. If you specify a hyphen,
                        //The value inside the brackets becomes a range. e.g. [a-d]bc matches
                        //abc, bbc, cbc and dbc

                        var ch = *pStr;

                        if (ch == '\0')
                            return true;

                        if (!fCase)
                        {
                            if (unchecked((uint) (ch - (byte) 'A') <= ('Z' - 'A')))
                                ch = (char) (ch | 0x20);
                        }

                        var bracketedLowerBounds = '\0';

                        //e.g. if you have [ab] bracketedChar might be the "a", and we're now going to try and match the "a" with the current character in the string
                        var bracketedChar = nextToken(&pRE, fCase);

                        if (bracketedChar == REGEX_ACTION_END)
                            continue;

CheckForCloseBracket:
                        if (bracketedChar == REGEX_ACTION_CLOSE_BRACKET)
                        {
                            if (nextToken(&pRE, fCase) != REGEX_ACTION_HASH)
                                return true;

                            MoveNext(bracketedChar, &pRE, fCase);
                            continue;
                        }
                        if (bracketedChar == '-') //If a '-' is specified, the value within the brackets is a range
                        {
                            var bracketedUpperBounds = nextToken(&pRE, fCase);
                            bracketedChar = bracketedUpperBounds;

                            if ((bracketedUpperBounds & 0xFFFB) == 0)
                                return true;

                            if (ch >= bracketedLowerBounds && ch <= bracketedUpperBounds)
                            {
                                ++pStr;

                                MoveNext(bracketedChar, &pRE, fCase);
                                continue;
                            }
                        }

                        bracketedLowerBounds = bracketedChar;

                        //If we have "bbc" and "[ab]bc", if the "a" doesn't match, get the next character (the "b") and try match that instead
                        if (ch != bracketedChar)
                        {
                            bracketedChar = nextToken(&pRE, fCase);

                            if (bracketedChar == REGEX_ACTION_END)
                                continue;

                            goto CheckForCloseBracket;
                        }

                        ++pStr;

                        if (bracketedChar != REGEX_ACTION_END)
                            MoveNext(bracketedChar, &pRE, fCase);

                        continue;
                    }
                        
                    case REGEX_ACTION_PLUS:
                    case REGEX_ACTION_HASH: //With optional prefix. i.e. a#b matches "b", "ab" but not "a", which is the optional prefix part
                        if (!CompareRE(pStr, pRE, fCase))
                            return false;

                        pRE = regexFromPreviousLoop;
                        continue;
                    default:
                    {
                        /* Whenever we encounter a wildcard, the wildcard case takes over all processing
                         * until we reach the end of the string. On the inner call to CompareRE, pStr is still
                         * on the previous character before the *. e.g. if we have abc / a*c, *pStr will be on
                         * the "b" while processing the wildcard. Because of this action will not match _ch below, and we'll
                         * return immediately. The case for handling wildcards will keep calling CompareRE for each successive
                         * character, until we hit the end of the string. If yet another wildcard is encountered, the nested
                         * wildcard case will then start doing the same thing as the parent, and the string processing will have
                         * been taken over yet again */
                        var ch = *pStr;

                        if (!fCase)
                        {
                            if (unchecked((uint) (ch - (byte) 'A') <= ('Z' - 'A')))
                                ch = (char) (ch | 0x20);
                        }

                        if (action == ch)
                        {
                            ++pStr;
                            continue;
                        }
                        if (nextToken(&pRE, fCase) == REGEX_ACTION_HASH)
                            continue;

                        return true;
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void MoveNext(char subAction, char** _pRE, bool fCase)
        {
            do
            {
                if (subAction == REGEX_ACTION_CLOSE_BRACKET)
                    break;

                subAction = nextToken(_pRE, fCase);
            }
            while (subAction != REGEX_ACTION_END);
        }

        //fCase = case sensitive
        static char nextToken(char** str, bool fCase)
        {
            var pch1 = *str;

            var ch1 = *pch1;

            if (ch1 == 0)
                return REGEX_ACTION_END;

            switch (ch1)
            {
                case '\0':
                    return REGEX_ACTION_END;
                case '#':
                    (*str)++;
                    return REGEX_ACTION_HASH;
                case '*':
                    (*str)++;
                    return REGEX_ACTION_ASTERISK;
                case '+':
                    (*str)++;
                    return REGEX_ACTION_PLUS;
                case '?':
                    (*str)++;
                    return REGEX_ACTION_QUESTION;
                case '[':
                    (*str)++;
                    return REGEX_ACTION_OPEN_BRACKET;
                case '\\': //Match special control characters used by CompareRE (i.e. any of the other special characters mentioned in this switch)
                    (*pch1)++;
                    *str = pch1;

                    if (*pch1 == 0)
                        return REGEX_ACTION_END;

                    goto label;
                case ']':
                    (*str)++;
                    return REGEX_ACTION_CLOSE_BRACKET;
                default:
                    label:
                    var result = *pch1;

                    if (!fCase)
                    {
                        if (unchecked((uint) (result - (byte) 'A') <= ('Z' - 'A')))
                            result = (char) (result | 0x20);
                    }

                    (*str)++;

                    return result;
            }
        }

        #endregion
    }
}
