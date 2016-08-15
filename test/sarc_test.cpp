// sarc_test.cpp : コンソール アプリケーションのエントリ ポイントを定義します。
//

#define _CRT_SECURE_NO_WARNINGS

#include "stdafx.h"
#include <stdio.h>
#include <cassert>
#include <memory>
#include <utility>
#include <sarc/archive.hpp>

int main()
{
	//const char* path = "G:\\work\\projects\\sarc\\test_data\\test01.sarc";
	const char* path = "G:\\work\\projects\\sarc\\SimpleArchive\\bin\\Debug\\test.arc";

	std::unique_ptr<std::uint8_t[]> buffer;
	std::uint32_t file_size = 0;
	{
		// ファイルを開く
		FILE* fp = fopen(path, "rb");

		// ファイルサイズを取得
		fseek(fp, 0, SEEK_END);
		fpos_t pos = 0;
		fgetpos(fp, &pos);
		file_size = static_cast<std::uint32_t>(pos);

		// ファイルの全内容を読み取る
		buffer.reset(new std::uint8_t[file_size]);
		fseek(fp, 0, SEEK_SET);
		fread(buffer.get(), 1, file_size, fp);

		// ファイルを閉じる
		fclose(fp);
	}

	sarc::Archive arc(buffer.get(), file_size);
	arc.get_file(0);

	for (sarc::FileAccessor file : arc)
	{
		printf("%s : %lu\n", file.file_name(), file.file_size());
	}

	// 比較演算子のテスト
	{
		auto it1 = arc.begin();
		auto it2 = arc.end();
		assert(it1 == it1);
		assert(it2 == it2);
		assert(it1 != it2);
		assert(it2 != it1);

		assert(it1 < it2);
		assert(!(it2 < it1));
		assert(it1 <= it2);
		assert(it1 <= it1);
		assert(!(it2 <= it1));

		assert(it2 > it1);
		assert(!(it1 > it2));
		assert(it2 >= it1);
		assert(it1 >= it1);
		assert(!(it1 >= it2));
	}

    return 0;
}
