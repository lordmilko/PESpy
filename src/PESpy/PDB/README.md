The PDB File Format is based on a "multistream file" concept, wherein there are multiple "streams" of data present in the file, each spread across a number of "pages". The PDB keeps track of which pages are in use and belong to which stream. The size that should be used for each page is determined when the PDB is first created.

The following table shows the page layout of a simple PDF created with `mspdbcore!MSFOpenW(wszFilename: "C:\\test.pdb", fWrite: true)`

| Address       | Page | Description            |
|---------------|------|------------------------|
| 0x0-0x3FF     | 0    | Master Index           |
| 0x400-0x7FF   | 1    | FPM 0 (1/1) (Active)   |
| 0x800-0xBFF   | 2    | FPM 1 (Inactive)       |
| 0xC00-0xFFF   | 3    | Stream Table (1/1)     |
| 0x1000-0x13FF | 4    | Stream Table Page List |
| 0x1400-0x17FF | 5    | Free                   |
| 0x1800-0x1BFF | 6    | Free                   |
| 0x1C00-0x1FFF | 7    | Free                   |
| 0x2000-0x23FF | 8    | Free                   |
| 0x2400-0x27FF | 9    | Free                   |
| 0x2800-0x2BFF | 10   | Free                   |

Unless otherwise specified, `MSFOpenW` creates a PDB that uses 1024 byte (0x400) pages. A custom page size can be specified by creating your PDB with `MSFOpenExW` instead.

The default number of pages that should be allocated in a brand new PDB is determined based on the desired page size. `mspdbcore!rgmsfparms_hc` defines a series `MSFParms` structures which list the default values for various parameters that should be used based on the selected page size. In the case of 1024 byte pages, 10 additional pages should be allocated by default (for a total of 11).

## Page 0: Master Index

PDB files always begin with the "master index" page at page 0, which contains the MSF Header that describes the PDB File. There are two main file formats that are found in the wild today

* PDB v2.0
* PDB v7.0

A PDB is v2.0 if it begins with the magic string `Microsoft C/C++ program database 2.00\r\n\x1a\x4a\x47` and is v7.0 if it begins with the magic string `Microsoft C/C++ MSF 7.00\r\n\x1a\x44\x53`. It is important to note however that it seems like there might be some alignment craziness that needs to be taken into consideration; the v7.0 magic string is not actually 30 bytes, but rather is 32. In C#, it is denoted as follows `Microsoft C/C++ MSF 7.00\r\n\u001aDS\0\0\0`.

All modern PDBs use PDB v7.0, which are referred to as "big" or "high capacity" PDBs. While PDB v2.0 files begin with a `MSF_HDR` struct, PDB v7.0 files begin with a `BIGMSF_HDR` structure. In a minimal PDB, the master index page has the following layout

| Address    | Description           |
|------------|-----------------------|
| 0x0-0x37   | BIGMSF_HDR            |
| 0x38-0x3FF | [Padding] Bytes (968) |

The default `BIGMSF_HDR` has the following structure

| Address   | Field     | Value                                      |
|-----------|-----------|--------------------------------------------|
| 0x0-0x1F  | szMagic   | Microsoft C/C++ MSF 7.00\r\n\u001aDS\0\0\0 |
| 0x20-0x23 | cbPg      | 1024                                       |
| 0x24-0x27 | pnFpm     | 1                                          |
| 0x28-0x2B | pnMac     | 11                                         |
| 0x2C-0x33 | siSt      | SI_PERSIST { cb = 4, mpspnpn = 0 }         |
| 0x34-0x37 | mpspnpnSt | 4                                          |

The meanings of these fields are as follows
* `szMagic`: identifies the PDB as a v7.0 PDB
* `cbPg`: the number of bytes per page. Since no explicit size was specified, the default size of 1024 bytes was used
* `pnFPm`: the page number of the active *Free Page Map* (discussed in the next section below)
* `pnMac`: the total (maximum) number of pages contained this PDB. As a page size of 1024 bytes was selected, 10 additional pages were allocated by default for a total of 11. `microsoft-pdb` seems to have a habit of spelling what I assume to be "max" as "mac"
* `siSt`: lists the number of bytes that the "stream table" spans. See *Stream Table* below for more info on this field
* `mpspnpnSt`: lists the page(s) that lists the pages that the stream table spans. See *Stream Table* below for more info on this field.

`microsoft-pdb` uses a litany of cryptic types and typedefs for the various pieces of data it needs to model. The following table lists some commonly seen types and what their meanings are

| Type | Meaning               | Description                                                                                                                                                    |
|------|-----------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------|
| PN   | Page Number           | The index of a page in the PDB. e.g. `PN` `0` is the master index, `PN` `1` is FPM 0, etc                                                                      |
| SI   | Stream Info           | Represents a stream in the PDB. Encapsulates the number of bytes in the stream, and a `PN[]` describing the pages that the stream's contents are spread across |
| SPN  | Stream Page Number    | Perhaps the most questionable of all typedefs, a `SPN` is simply...an index into the `SI` `PN[]` page list. So instead of doing `for (int i = 0; i < si.PageList.Count; i++) { si.PageList[i] }` you instead do `for (SPN i = 0; i < si.PageList.Count; i++) { si.PageList[i] }`
| SN   | Stream Number         | The index of a stream in the PDB. In a normal PDB, SNs 0-5 are reserved for special streams (like the DBI). Streams >5 can be used for anything. When dealing with the MSF API directly however, you are free to do whatever you like, starting from SN 1
| PN32 | Page Number (32-bit)  | A 32-bit Page Number. `PN` is 16-bit
| UPN  | Universal Page Number | Also a 32-bit Page Number. `PN`, `PN32` and `UPN` are interchangably used all over the place

### Stream Table

The Stream Table describes the following pieces of information
* How many streams are present in the PDB? (`int`)
* How many pages does each stream require? (`int[]`)
* What are the actual pages that each stream spans? (`PN[][]`)

In a sufficiently large PDB, actually *storing the information **about the stream table itself*** could potentially require the use of multiple pages. Thus, the process of reading the stream table involves a layer of indirection, as follows:

1. Identify the page containing the *list of pages that comprise the stream table*
2. Read the stream table information from each of those pages
3. Reconstruct `SI` (Stream Info) structs from the deserialized information

The `SI_PERSIST siSt` and `PN32[] mpspnpnSt` members provide us with the information required to achieve Step 1. Deciphering the meaning of these names and what their actual purpose is from the cryptic `microsoft-pdb` codebase is probably *the most* challenging aspect about understanding the PDB file format.

And so, before we can try and make sense of these, we need to first understand what a `SI` (Stream Info) struct is.

#### Understanding SI

As discussed above, the PDB file format consists of a variety of "streams", with each stream spread across multiple pages. A `SI` (Stream Info) struct contains the following members
* `int cb`: the number of bytes in the stream
* `PN[] mpspnpn`: a "map" of SPN -> PN

It is quite confusing trying to understand how exactly `mpspnpn` is a "map"; the key is to understand that a `SPN` is literally just "an array index for a PN[]". Thus, you can reinterpret this member as follows
* Map of SPN -> PN
* = Map of array index to PN
* = *Literally just an array of PN!*

Thus, a Stream Info struct can be understood to contain the following members
* `int ByteCount`: the number of bytes across all pages in the stream
* `PN[] PageList`: the list of pages that the stream spans across

`SI` (Stream Info) structs only exist in memory. They are synthesized from the information contained in the stream table (described below).

## Understanding The Stream Table

Now that we understand what an `SI` is, we should be able to understand what an `SI_PERSIST` is: it's a stream that has been physically persisted to disk! No need to manually reconstruct it from a byte count stored in one location and a `PN[]` stored in another. It's purpose: to tell us all of the pages that the stream table is spread across, so that we can read and re-construct it

Or at least, that's the theory. When we look at `SI_PERSIST siSt` (siSt = Stream Info for the Stream Table) we see have the following
* `cb` (ByteCount): 4
* `mpspnpn` (PageList): 0

The PageList is null! For some reason, the PageList is not stored inline in the `SI_PERSIST` struct. The PageList is instead stored *next to* the `SI_PERSIST` struct, in the `mpspnpnSt` field.
* mpspnpnSt = "map" of SPN -> PN for the Stream Table
* = Map of array index of PN for the Stream Table
* = *Literally just an array of the PNs for the Stream Table!*

To understand how `mpspnpnSt` works, consider the following:

* There exists a Stream Table that is 307,528 bytes large in a PDB that uses 1024 byte pages
* The Stream Table itself therefore spans 301 pages (307,528 / 1024)
* Each page number occupies 4 bytes
* Therefore, listing the location of the Stream Table's 301 pages requires 1204 bytes
* Which means that the list of pages that comprise the Stream Table itself spans 2 pages (1204 / 1024)

The `microsoft-pdb` source code indicates that `mpspnpnSt` could be an array, however it is hard to decipher under what circumstance it could ever contain more than one value amidst the mess of legacy C++ code. Other third party PDB readers assume that `mpspnpnSt` only ever contains a single value. In practice, it seems that when the stream table page list spans multiple pages, these pages are sequential. Therefore, you can technically get away with only reading the first page listed in `mpspnpnSt`, and as you read "beyond" the end of the first page you will inadvertently correctly read into all subsequent pages. You can stress test creating a PDB whose stream table page list spans more than 1 page using `mspdbcore.dll` and creating a PDB with 75 streams, each with 1,048,576 bytes of junk in each stream, resulting in a ~77mb PDB.

Confusingly, microsoft-pdb has the following remark in `msf.cpp`:

> Also, a layer of indirection has been added to the stream table serialization. Where before the page list for the stream table was stored in the header page for the reconstruction of the stream table, now a page list of the pages is written instead. This way the page list for the stream table won't exceed a single page.

As evidenced by the stress test I have performed, assuming my interpretation of what they are saying is right, this statement is incorrect.

Overall, reading the stream table (using `SI_PERSIST.cb` coupled with the singular page listed in `mpspnpnSt`) we get the information we need to reconstruct the in-memory `SI` structs.

## The Stream Table Stream

There is one more wrinkle to understand when it comes to the Stream Table. The first valid number you can use for a stream is 1 (`snUserMin`). SN 0 cannot be used because SN 0 has a special meaning: `snSt`...the stream that stores the stream table!

Before you lose your mind, that this stream table madness never ends, fear not! What `snSt` really does (when it comes to reading the data on disk) is it stores a backup of the *previous* stream table.

To illustrate this, consider the following
1. create a brand new PDB (via `MSFOpenW`). The Stream Table is empty
2. open the PDB again, and call `MSF::ReplaceStream(1, 0, 0)`, `MSF::Commit` and `MSF::Close`.
3. When you read the Stream Table from the `BIGMSF_HDR` you will find there are now two streams: Stream 1 (which we just added) and also Stream 0, which points to some data on Page 3
4. When you read the Stream Table from page 3, you will see the Stream Table that existed in Step 1: an empty stream table
5. Open the PDB again, do another `Commit` and `Close`
6. the `BIGMSF_HDR` Stream Table now says that Stream 0 exists on Page 5
7. And the Stream Table on Page 5 now contains a copy of the `BIGMSF_HDR` that existed in Step 3

Perhaps erroneously, the page(s) that `snSt` spans appear be marked as free in the FPM. In one sense it might be true that they're free (in that the FPM may use them for something else) however in another sense, by virtue of the page being listed in a stream, there *must be* meaningful data there that a PDB reader should be able to read and understand

## Free Page Map

The Free Page Map (FPM) functions like a "memory manager" for the PDB file. When a new piece of data needs to be stored, the PDB file will ask the FPM to allocate a page for it. Physically, the FPM is represented as a series of bits, with a `1` meaning that the page is free and a `0` meaning that the page is already in use.

In a minimal PDB, the FPM has the following layout:

| Byte | Hex  | Binary   |
|------|------|----------|
| 0    | 0xE0 | 11100000 |
| 1    | 0xFF | 11111111 |
...
| 1023 | 0xFF | 11111111 |

We can see that the first 5 bits (read right to left) are 0, meaning that the first 5 pages have already been allocated. This matches the first table we saw above, where the first 5 pages (0-4) have data in them, whereas pages 5-10 are free.

Each PDB contains two FPMs. The first page of the "active" one is pointed to by the `BIGMSF_HDR`. Whenever you do a `MSF_HB::Commit`, `serializeFpm` serializes the current FPM, and then `MSF_HB::init` is called which swaps the active FPM on the `BIGMSF_HDR` - however, this only applies in memory. This will be the FPM that is used for the next commit that happens. The FPM on disk is still whatever the previous one was. In theory, the dual FPMs allow for incremental and atomic updates: you can do whatever you like, but your changes won't take effect until you say to use the new FPM page. The "fpm" member in the MSF type in `microsoft-pdb` just seems to store the current in memory representation. The `fpm` member doesn't otherwise appear to do anything special, nor get used for rolling back uncommitted changes.

The pages that FPM0 and FPM1 get stored in are based on the `MSFParms` structure that is associated with the selected page size. In the case of high capacity PDBs, FPM0 is always page 1 and FPM1 is always page 2. There are also multiple asserts all over `microsoft-pdb` that ensure that this is the case. Only legacy v2 PDBs have different pages for their FPMs based on their page sizes.

When a PDB is sufficiently large, you may need multiple FPM pages to store bits for each page in the file. With 1024 byte pages, you can store 8192 bits/pages in a single 1024 byte FPM page. Thus, in theory, you would only need to allocate a second FPM page once your PDB reaches at least 8192 pages.

Given that FPM0 is on page 1 and FPM1 is on page 2, you can calculate whether a given page should be reserved for use by the FPM using the following expression

```
pageNumber % (pageSize * 8)
```

e.g. if we have 1024 byte pages:

```
Page 1:       1 % 8192 = 1 (FPM Page)
Page 3:       3 % 8192 = 3 (Non-FPM Page)
Page 8193: 8193 % 8192 = 1 (FPM Page)
```

However, microsoft-pdb has a bug in its formula. Instead of doing `pageNuber % (pageSize * 8)`, they do `pageNumber % pageSize`

Which means we end up with the following

```
Page 1:       1 % 1024 = 1 (FPM Page)
Page 1025: 1025 % 1024 = 1 (FPM Page)
Page 2049: 2049 % 1024 = 1 (FPM Page)
...
Page 8193: 8193 % 8192 = 1 (FPM Page)
```

We end up reserving 8x too many pages for use by the FPM! This bug is propagated everywhere that deals with writing data to the FPM. serializeFpm() correctly identifies the correct _number_ of pages that the FPM needs to span, but still has to utilize the buggy logic of writing the first set of data into Page 1, and the second into Page 1025, etc