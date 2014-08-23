Portable.Data.Sqlite - Portable ADO-style SQLite with Encryption
================================================================

This is a portable cross-platform ADO provider for SQLite databases, featuring table-column-level and table-record-level data encryption. 

If you are looking for information about this library and how to use it, or the source code - please start with the project site at:
https://github.com/ellisnet/Portable.Data.Sqlite

IMPORTANT NOTES - PLEASE READ THESE:

1) This library works with, and requires, the Portable Class Library for SQLite (SQLitePCL) from MSOpenTech.  It is available via NuGet -
    search for 'SQLitePCL'.

	Here is the SQLitePCL project site:
	http://sqlitepcl.codeplex.com/
	Here is a page with excellent information about how to configure SQLitePCL for different platforms:
	https://sqlitepcl.codeplex.com/documentation

	REMEMBER: For Xamarin.iOS, you will need to call SQLitePCL.CurrentPlatform.Init() before you begin accessing any SQLite databases.

2) This library (Portable.Data.Sqlite) *enables* SQLite data encryption, but doesn't perform the actual data encryption.  I.e. it doesn't 
	come with an encryption algorithm in the library.  All supported OS platforms come with some type of built-in encryption 
	functionality, so you can use that - or you can look into Portable.BouncyCastle - which is available here:  
	http://www.nuget.org/packages/Portable.BouncyCastle/

	To allow this library (Portable.Data.Sqlite) to carry out the necessary data-encryption functions, you will need to pass in an 
	object that implements the IObjectCryptEngine interface.  It must have EncryptObject() and DecryptObject<T>() methods that do the
	encryption/decryption.  That is really the only (potentially) difficult part of using this library, and it is pretty straight-forward.
	There is good information about implementing IObjectCryptEngine at the project site:  
	https://github.com/ellisnet/Portable.Data.Sqlite

The developer of this library welcomes all feedback, suggestions, issue/bug reports, and pull requests. Please log questions and issues 
in the Portable.Data.Sqlite GitHub 'Issues' section - available at: https://github.com/ellisnet/Portable.Data.Sqlite/issues

This library is licensed under a permissive Apache 2.0 open source license - available at the project site.

This library includes code from the Mono implementation of System.Data and from a library called Mono.Data.Sqlite -
    see below for the licenses for this code.  It also includes tweaks to Mono.Data.Sqlite from Matthew Leibowitz (@mattleibow), 
	who seems to have done a lot of work to enable access to SQLite from portable class libraries (including contributions to
	SQLitePCL).  His work is here:  https://github.com/mattleibow/Mono.Data.Sqlite

====================================================================
License information for (Mono) System.Data and Mono.Data.Sqlite code
====================================================================

The license/copyright information provided with [the Mono implementation 
of] System.Data is as follows:

Copyright (C) 2009 Novell, Inc (http://www.novell.com)
[Some portions] Copyright (C) Ximian, Inc (http://www.ximian.com)
[Some portions] Copyright (C) Mainsoft, Inc (http://www.mainsoft.com)
[Some portions] Copyright (C) Ben Maurer (bmaurer@users.sourceforge.net)
[Some portions] Copyright (C) Chris Podurgiel (cpodurgiel@msn.com)
[Some portions] Copyright (C) Tim Coleman (tim@timcoleman.com)
[Some portions] Copyright (C) Ville Palo (vi64pa@kolumbus.fi)

Permission is hereby granted, free of charge, to any person obtaining a 
copy of this software and associated documentation files (the "Software"), 
to deal in the Software without restriction, including without limitation 
the rights to use, copy, modify, merge, publish, distribute, sublicense, 
and/or sell copies of the Software, and to permit persons to whom the 
Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in 
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE 
SOFTWARE.


The license information provided with Mono.Data.Sqlite is as follows:

ADO.NET 2.0 Data Provider for SQLite Version 3.X
Written by Robert Simpson (robert@blackcastlesoft.com)

Released to the public domain, use at your own risk!

