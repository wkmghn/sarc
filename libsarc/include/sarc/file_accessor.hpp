//
// Copyright (c) 2016 wkmghn.
// 
// Use, modification and distribution is subject to the Boost Software License,
// Version 1.0. (See accompanying file LICENSE_1_0.txt or copy at
// http://www.boost.org/LICENSE_1_0.txt)
//
#pragma once

#include <cstdint>

namespace sarc
{

class Archive;

/// アーカイブ内のファイルへのアクセスを提供します。
/// このクラスのインスタンスは無効な状態を表現できます。
class FileAccessor
{
public:

	/// 無効な FileAccessor として初期化します。
	FileAccessor()
		: file_body_(nullptr), file_name_(nullptr), file_size_(0) { }

	/// 無効な FileAccessor として初期化します。
	FileAccessor(std::nullptr_t)
		: FileAccessor() { }

	FileAccessor(const FileAccessor&) = default;
	FileAccessor& operator=(const FileAccessor&) = default;

	/// 有効な FileAccessor であるかを示す値を取得します。
	bool is_valid() const { return file_body_ != nullptr; }

	/// ファイルの実データへのポインタを取得します。
	/// 現在のインスタンスが無効な場合は null を返します。
	/// ファイルサイズがゼロの場合、null ではないポインタを返しますが正常なデータを指しているわけではありません。
	const void* data() const { return is_valid() ? file_body_ : nullptr; }

	/// ファイル名を取得します。
	/// 現在のインスタンスが無効な場合は null を返します。
	const char* file_name() const { return is_valid() ? file_name_ : nullptr; }

	/// ファイルサイズを取得します。
	/// 現在のインスタンスが無効な場合はゼロを返します。
	std::uint32_t file_size() const { return is_valid() ? file_size_ : 0; }

private:

	friend class Archive;
	FileAccessor(const void* file_body, const char* file_name, std::uint32_t file_size)
		: file_body_(file_body), file_name_(file_name), file_size_(file_size) { }

private:

	const void* file_body_;
	const char* file_name_;
	std::uint32_t file_size_;

};

}  // namespace sarc
