readme.txt
                                                                     2009-02-25
                                                                YAMAMOTO Suguru
                                          http://sourceforge.jp/projects/azuki/

== Package contents ==

- Azuki.dll
	.NET Framework version of Azuki.
	To use Azuki in a desktop application,
	please reference this DLL.

- AzukiCompact.dll
	.NET Compact Framework version of Azuki.
	To use Azuki in a mobile application,
	please reference this DLL.

- Ann.exe
	Sample program for testing .NET Framework version of Azuki.
	This may work under Windows 2000, XP, Vista.

- AnnCompact.exe
	Sample program for testing .NET Compact Framework version of Azuki.
	To execute this program, please copy this executable with
	AzukiCompact.dll to a Windows Mobile device and execute
	(currently this is only tested with SHARP's Advanced W-ZERO3 [es]).

- Azuki.xml
	XML document of Azuki.dll.
	In case of developing an application
	using Azuki with IDE like Visual Studio,
	please copy this file with Azuki.dll to same directory.

- AzukiCompact.xml
	XML document of AzukiCompact.dll.
	In case of developing an application
	using Azuki with IDE like Visual Studio,
	please copy this file with AzukiCompact.dll to same directory.

== about assembly sign of Azuki.dll ==
This package contains Azuki.dll which are digitally signed.
The reason why it is signed is,
they can be referenced by other signed assembly if it is signed.
Ensuring that the assemblies are not altered is NOT the reason.
If you will be needed to prove that
the Azuki.dll you are using is not altered,
you SHOULD build another Azuki.dll from source code package
with your own key file.
