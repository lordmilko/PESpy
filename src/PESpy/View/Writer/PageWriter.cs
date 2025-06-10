using System;
using System.Collections.Generic;
using PESpy.PDB;

namespace PESpy.View
{
    public partial class ViewWriter
    {
        //Provides facilities for writing arrays of values that may potentially span multiple pages
        internal ref struct PageWriter
        {
            private readonly ViewWriter viewWriter;
            private readonly bool global;
            private readonly bool shouldAdd;

            //The current offset within the current page. i.e. if the page size
            //is 0x1000 this is a value between 0-0xFFF. When we move into another page,
            //this value is reset to the new offset in the next page (after factoring in overflow)
            private int relativeOffset;
            private int pageStart; //The absolute position that the current page starts at. The address of the next value is pageStart + relativeOffset
            private int pageIndex;

            private readonly int pageSize;
            private readonly PN[] pageList;

            private List<IView> items;
            private bool end;

            internal PageWriter(int startRelativeOffset, PagedMemoryBlock block, ViewWriter viewWriter, bool global, bool shouldAdd)
            {
                relativeOffset = startRelativeOffset;
                pageSize = block.pageSize;

                pageList = block.pageList;

                pageIndex = startRelativeOffset / pageSize;
                pageStart = pageList[pageIndex] * pageSize;

                this.viewWriter = viewWriter;
                this.global = global;
                this.shouldAdd = shouldAdd;

                items = viewWriter.RentList();
                end = false;
            }

            public void WriteValue<T>(in T value, int size, ViewKind kind)
            {
                //If we overflow the end of the page, merger will split us
                items.Add(new ValueView<T>(pageStart + relativeOffset, value, size, kind));
                relativeOffset += size;

                if (relativeOffset >= pageSize)
                {
                    //Move onto the next page
                    pageIndex++;

                    if (pageIndex >= pageList.Length)
                    {
                        end = true;
                        return;
                    }

                    pageStart = pageList[pageIndex] * pageSize;

                    //Adjust for any overflow
                    relativeOffset -= pageSize;
                }
            }

            public void Dispose()
            {
                if (shouldAdd)
                {
                    viewWriter.AddViews(items);
                }

                viewWriter.ReturnList(items);

                if (global)
                    viewWriter.Pop(); //Remove the global scope
            }
        }
    }
}
