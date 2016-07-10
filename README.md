FVAnalyzer
==========
[![MIT License](http://img.shields.io/badge/license-MIT-blue.svg?style=flat)](LICENSE)

fv.binなどの統計情報を表示したりするツール


exe自体か、起動後のウィンドウにファイルやフォルダをドロップすると、最大値とかが表示されます。

大きいファイルはOutOfMemoryでエラーになりやすかったりとかその他色々適当ですが、とりあえず。


対応ファイル形式
-----
- fv.binのように16bit符号付き整数値が連続しているバイナリファイル
- Blunder風(?)CSVファイル


表示される情報
-----
- 最小値
- 最大値
- 絶対値の最大値(最小値*(-1) or 最大値)
- -1～+1の要素を除く要素の平均値
- -1～+1の要素を除く要素の二乗の総和の平方根
- -1～+1の要素を除く要素の絶対値の平均
- -1～+1の要素を除く要素の全体に対する数の割合



最新バイナリ
------
https://github.com/ak110/FVAnalyzer/blob/master/Binary/FVAnalyzer.exe?raw=true
