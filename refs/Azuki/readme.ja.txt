readme.ja.txt
                                                                     2009-05-26
                                                                YAMAMOTO Suguru
                                                   http://azuki.sourceforge.jp/

== 同梱物の説明 ==

- Azuki.dll
	.NET Framework 版の Azuki です。
	デスクトップアプリケーションで Azuki を使う場合は
	この DLL を参照します。

- AzukiCompact.dll
	.NET Compact Framework 版の Azuki です。
	モバイルアプリケーションで Azuki を使う場合は
	この DLL を参照します。

- Ann.exe
	.NET Framework 版 Azuki の動作確認用サンプルプログラムです。
	おそらく Windows 2000、XP、Vista で動作します。

- AnnCompact.exe
	.NET Compact Framework 版 Azuki の
	動作確認用サンプルプログラムです。
	動作を確認したい方は AzukiCompact.dll と
	このファイルを Windows Mobile 機にコピーして
	実行してみてください
	（現在 SHARP 製 Advanced W-ZERO3 [es] でのみ動作確認）。

- Azuki.xml
	Azuki.dll の XML ドキュメントファイルです。
	Visual Studio などの統合開発環境で使う場合は
	Azuki.dll と同じディレクトリにセットでコピーします。

- AzukiCompact.xml
	AzukiCompact.dll の XML ドキュメントファイルです。
	Visual Studio などの統合開発環境で使う場合は
	AzukiCompact.dll と同じディレクトリにセットでコピーします。

== Azuki.dll のアセンブリ署名について ==
パッケージに付属している Azuki.dll には「アセンブリ署名」を行っています。
この目的は、アセンブリ署名を行っている別プロジェクトから参照可能にすることであり、
アセンブリの不正な変更を防止することではありません。
もし、 Azuki.dll を使用するとそれが不正に変更されていないことを
証明しなければならなくなると予想される場合、
独自のキーを使って Azuki.dll をソースからビルドして署名すべきでしょう。
