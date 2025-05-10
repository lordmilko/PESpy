#if FALSE //This allocates way too much memory, regardless of whether we use a dictionary or an array at each level. Furthermore, we allocate 17,000 node objects just for the 8 patterns we want to match against
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace PESpy
{
    //Based on SequenceSearchState.java from Ghidra, licensed under the Apache License.
    //See ThirdPartyNotices.txt for full license notice.

    /// <summary>
    /// Provides facilities for simultaneously comparing a byte value against multiple possible <see cref="ByteSequence"/> values
    /// by constructing a tree that can be traversed to "dig down" to possible patterns that may match. e.g. given the patterns
    /// 0xff00 and 0xff01, this will result in three tree nodes:
    ///     Root -> 0xff
    ///             0xff -> 0x00
    ///             0xff -> 0x01
    /// </summary>
    class ByteSequenceTreeNode
    {
        private const int LevelWidth = 256;

        /// <summary>
        /// Gets all sequences that could still match at this level of the tree,
        /// or on any of its sub nodes.
        /// </summary>
        public List<ByteSequence> Candidates { get; } = new List<ByteSequence>();

        /// <summary>
        /// Gets all sequences that have been completely matched against
        /// at this level of the tree.
        /// </summary>
        public List<ByteSequence> Complete { get; } = new List<ByteSequence>();

        //Using array with all 256 bytes at each level results in an explosion of memory (40mb). Using a dictionary instead is still pretty bad: 13mb.
        //The crux of the issue is we have a combinatoric explosion and possibilities, and get a tree with over 17,000 nodes in it
        public Dictionary<byte, ByteSequenceTreeNode?>? ChildNodes { get; private set; }

        public int Depth { get; }

        public ByteSequenceTreeNode? Parent { get; }

        public byte? MatchedByte { get; }

        private ByteSequenceTreeNode(int depth, ByteSequenceTreeNode? parent, byte? matchedByte)
        {
            Depth = depth;
            Parent = parent;
            MatchedByte = matchedByte;
        }

        public ByteSequenceTreeNode? this[byte index]
        {
            get
            {
                ByteSequenceTreeNode? value = null;
                ChildNodes?.TryGetValue(index, out value);
                return value;
            }
        }

        public static ByteSequenceTreeNode BuildTree(params ByteSequence[] patterns)
        {
            var root = new ByteSequenceTreeNode(0, null, null);

            foreach (var pattern in patterns)
                root.Add(pattern);

            BuildLevels(root);

            return root;
        }

        private void Add(ByteSequence sequence)
        {
            Candidates.Add(sequence);

            if (sequence.Length == Depth)
                Complete.Add(sequence);
        }

        private static void BuildLevels(ByteSequenceTreeNode root)
        {
            var byteDepth = 0;

            var current = new[] { root };

            List<ByteSequenceTreeNode> nextLevels = new List<ByteSequenceTreeNode>();

            do
            {
                foreach (var item in current)
                {
                    for (var i = 0; i < LevelWidth; i++)
                    {
                        ByteSequenceTreeNode? nextLevel = null;

                        foreach (var candidate in item.Candidates)
                        {
                            if (candidate.HasByte(byteDepth, (byte) i))
                            {
                                //We need to be considered on this level!

                                if (nextLevel == null)
                                    nextLevel = new ByteSequenceTreeNode(byteDepth + 1, item, (byte) i);

                                nextLevel.Add(candidate);
                            }
                        }

                        if (item.ChildNodes == null)
                            item.ChildNodes = new Dictionary<byte, ByteSequenceTreeNode?>();

                        item.ChildNodes[(byte) i] = nextLevel;

                        if (nextLevel != null)
                            nextLevels.Add(nextLevel);
                    }
                }

                //When a sequence contains a pattern that matches any value in a certain position,
                //this can result in us getting a bunch of duplicates, since a sequence will respond
                //"oh yes I match that" against every single value that's shown to it at a given
                //byte position
                var dict = new Dictionary<ByteSequenceTreeNode, ByteSequenceTreeNode>(ByteSequenceTreeNodeEqualityComparer.Instance);

                var toRemove = new List<ByteSequenceTreeNode>();

                foreach (var item in nextLevels)
                {
                    if (dict.TryGetValue(item, out var existing))
                    {
                        //Ghidra normally uses a function called "merge" to merge items together when removing them. Until we've shown that we need that,
                        //we aren't going to worry about such complexity.
                        if (item.Complete.Count > 0 && !existing.Complete.All(v => item.Complete.Any(c => c == v)))
                            throw new NotImplementedException("Don't know how to remove a node whose completed items were different from its apparent duplicate.");

                        toRemove.Add(item);
                        item.Parent!.ChildNodes[item.MatchedByte!.Value] = existing;
                    }
                    else
                        dict[item] = item;
                }

                current = nextLevels.Except(toRemove).ToArray();
                nextLevels.Clear();

                byteDepth++;
            } while (current.Length > 0);
        }

        public IEnumerable<ByteMatch> GetMatches(IntPtr bytes, int numBytes, bool startOnly)
        {
            ByteSequenceTreeNode? current;

            for (var i = 0; i < numBytes; i++)
            {
                //Start at the root
                current = this;
                var j = i;

                do
                {
                    if (current.Complete.Count > 0)
                    {
                        foreach (var item in current.Complete)
                            yield return new ByteMatch(i, item);
                    }

                    //We traversed down so many nodes
                    //we've run out of bytes
                    if (j >= numBytes)
                        break;

                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    static unsafe byte GetByte(IntPtr bytes, int index)
                    {
                        return ((byte*) bytes)[index];
                    }

                    var currentByte = GetByte(bytes, j);

                    //For debugging
                    var previous = current;

                    current = current.ChildNodes[currentByte];
                    j++;
                } while (current != null);

                if (startOnly)
                    break;
            }
        }

        public unsafe IEnumerable<ByteMatch> GetMatches(Stream stream, bool startOnly)
        {
            var longestCandidate = Candidates.Max(c => c.Bytes.Length);

            var bytes = new byte[longestCandidate];

            var read = stream.Read(bytes, 0, longestCandidate);

            if (read != longestCandidate)
                Array.Resize(ref bytes, read);

            fixed (byte* p = bytes)
            {
                return GetMatches((IntPtr) p, bytes.Length, startOnly);
            }
        }

        public unsafe IEnumerable<ByteMatch> GetMatches(in MemoryChunk chunk, bool startOnly)
        {
            var longestCandidate = Candidates.Max(c => c.Bytes.Length);

            return GetMatches((IntPtr) chunk.Pointer, longestCandidate, startOnly);
        }

        public override string ToString()
        {
            if (Depth > 0)
            {
                var bytes = new List<byte>();

                bytes.Add(MatchedByte!.Value);

                var parent = Parent;

                while (parent != null && parent.MatchedByte != null)
                {
                    bytes.Add(parent.MatchedByte.Value);
                    parent = parent.Parent;
                }

                bytes.Reverse();

                return "0x" + string.Join(string.Empty, bytes.Select(v => v.ToString("X2")));
            }
            else
                return "Root";
        }
    }
}
#endif
