//
// Copyright (c) 2016 wkmghn.
// 
// Use, modification and distribution is subject to the Boost Software License,
// Version 1.0. (See accompanying file LICENSE_1_0.txt or copy at
// http://www.boost.org/LICENSE_1_0.txt)
//
#include <sarc/archive.hpp>
#include <cassert>
#include <cstring>

namespace sarc
{

namespace
{
// MagicNumber + Version + NumFiles
const std::uint32_t MINIMUM_ARCHIVE_SIZE = 4 + 4 + 4;

std::uint32_t read_uint32(const uint8_t* data)
{
	return static_cast<std::uint32_t>(data[0]) << 24
		| static_cast<std::uint32_t>(data[1]) << 16
		| static_cast<std::uint32_t>(data[2]) << 8
		| static_cast<std::uint32_t>(data[3]) << 0;
}
}

Archive::Archive(const void* data, std::uint32_t data_size)
	: parsing_result_(ParsingResult::Succeeded)
	, data_(reinterpret_cast<const std::uint8_t*>(data))
	, data_size_(data_size)
{
	if (data_ == nullptr) {
		parsing_result_ = ParsingResult::NullData;
		return;
	}

	if (data_size_ < MINIMUM_ARCHIVE_SIZE) {
		parsing_result_ = ParsingResult::TooFewDataSize;
		return;
	}

	// MagicNumber = 's' 'a' 'r' 'c'
	if (data_[0] != 0x73 || data_[1] != 0x61 || data_[2] != 0x72 || data_[3] != 0x63) {
		parsing_result_ = ParsingResult::DataCorrupted;
		return;
	}

	// Version
	const std::uint32_t version = read_uint32(data_ + 4);
	if (version != 1) {
		parsing_result_ = ParsingResult::UnsupportedVersion;
		return;
	}
}

Archive::~Archive() = default;

std::uint32_t Archive::num_files() const
{
	if (parsing_result_ != ParsingResult::Succeeded) {
		return 0;
	}
	assert(data_ != nullptr);

	return read_uint32(data_ + 8);
}

FileAccessor Archive::get_file(std::uint32_t file_index) const
{
	if (parsing_result_ != ParsingResult::Succeeded) {
		return nullptr;
	}
	assert(data_ != nullptr);

	if (num_files() <= file_index) {
		return nullptr;
	}

	const std::uint32_t offset_offset = 12 + file_index * 4;
	const std::uint32_t file_head_offset = read_uint32(data_ + offset_offset);

	const std::uint8_t* file_head = data_ + file_head_offset;
	// ファイルサイズがゼロでも何らかの値が読み取れる。
	// 少なくともアーカイブ内のどこかを指すが、まともなデータは読み取れない。
	const std::uint32_t file_body_offset_from_head = read_uint32(file_head);
	const std::uint32_t file_body_offset = file_head_offset + file_body_offset_from_head;
	const std::uint8_t* file_body = data_ + file_body_offset;

	const std::uint32_t file_size = read_uint32(file_head + 4);
	// 今のところランタイム側では参照しない
	//std::uint32_t alignment = read_uint32(file_head + 8);
	const char* file_name = reinterpret_cast<const char*>(file_head + 12);

	return FileAccessor(file_body, file_name, file_size);
}

FileAccessor Archive::find_file(const char* file_name) const
{
	if (parsing_result_ != ParsingResult::Succeeded) {
		return nullptr;
	}
	assert(data_ != nullptr);

	const std::uint32_t num = num_files();
	for (std::uint32_t i = 0; i < num; ++i) {
		FileAccessor file = get_file(i);
		assert(file.is_valid());
		if (std::strcmp(file.file_name(), file_name) == 0) {
			return file;
		}
	}

	return nullptr;
}

Archive::Iterator Archive::begin() const
{
	return Iterator(this, 0);
}

Archive::Iterator Archive::end() const
{
	return Iterator(this, num_files());
}

//==============================================================================
// Iterator
//==============================================================================

Archive::Iterator::Iterator()
	: archive_(nullptr)
	, file_index_(0)
{
}

Archive::Iterator::Iterator(const Archive* archive, std::uint32_t file_index)
	: archive_(archive)
	, file_index_(file_index)
{
	assert(archive != nullptr);
	assert(file_index <= archive->num_files());
}

Archive::Iterator::value_type Archive::Iterator::operator*() const
{
	assert(archive_ != nullptr);
	assert(file_index_ < archive_->num_files());
	return archive_->get_file(file_index_);
}

Archive::Iterator::value_type Archive::Iterator::operator[](difference_type index) const
{
	assert(archive_ != nullptr);
	const uint32_t new_file_index = static_cast<std::uint32_t>(file_index_ + index);
	assert(new_file_index <= archive_->num_files());
	return archive_->get_file(new_file_index);
}

Archive::Iterator& Archive::Iterator::operator++()
{
	assert(archive_ != nullptr);
	assert(file_index_ < archive_->num_files());
	++file_index_;
	return *this;
}

Archive::Iterator Archive::Iterator::operator++(int)
{
	assert(archive_ != nullptr);
	assert(file_index_ < archive_->num_files());
	++file_index_;
	return Iterator(archive_, file_index_ - 1);
}

Archive::Iterator& Archive::Iterator::operator--()
{
	assert(archive_ != nullptr);
	assert(0 < file_index_);
	--file_index_;
	return *this;
}

Archive::Iterator Archive::Iterator::operator--(int)
{
	assert(archive_ != nullptr);
	assert(0 < file_index_);
	--file_index_;
	return Iterator(archive_, file_index_ + 1);
}

Archive::Iterator& Archive::Iterator::operator+=(difference_type rhs)
{
	assert(archive_ != nullptr);
	file_index_ = static_cast<std::uint32_t>(file_index_ + rhs);
	assert(file_index_ <= archive_->num_files());
	return *this;
}

Archive::Iterator& Archive::Iterator::operator-=(difference_type rhs)
{
	assert(archive_ != nullptr);
	file_index_ = static_cast<std::uint32_t>(file_index_ - rhs);
	assert(file_index_ <= archive_->num_files());
	return *this;
}

Archive::Iterator operator+(const Archive::Iterator& lhs, Archive::Iterator::difference_type rhs)
{
	assert(lhs.archive_ != nullptr);
	const std::uint32_t file_index = static_cast<std::uint32_t>(lhs.file_index_ + rhs);
	assert(file_index <= lhs.archive_->num_files());
	return Archive::Iterator(lhs.archive_, file_index);
}

Archive::Iterator operator+(Archive::Iterator::difference_type lhs, const Archive::Iterator& rhs)
{
	return rhs + lhs;
}

Archive::Iterator operator-(const Archive::Iterator& lhs, Archive::Iterator::difference_type rhs)
{
	return lhs + (-rhs);
}

Archive::Iterator::difference_type operator-(const Archive::Iterator& lhs, const Archive::Iterator& rhs)
{
	assert(lhs.archive_ != nullptr);
	assert(rhs.archive_ != nullptr);
	return static_cast<Archive::Iterator::difference_type>(lhs.file_index_) - static_cast<Archive::Iterator::difference_type>(rhs.file_index_);
}

bool operator==(const Archive::Iterator& lhs, const Archive::Iterator& rhs)
{
	assert(lhs.archive_ != nullptr);
	assert(rhs.archive_ != nullptr);
	assert(lhs.archive_ == rhs.archive_);
	return lhs.file_index_ == rhs.file_index_;
}

bool operator!=(const Archive::Iterator& lhs, const Archive::Iterator& rhs)
{
	return !operator==(lhs, rhs);
}

bool operator<(const Archive::Iterator& lhs, const Archive::Iterator& rhs)
{
	assert(lhs.archive_ != nullptr);
	assert(rhs.archive_ != nullptr);
	assert(lhs.archive_ == rhs.archive_);
	return lhs.file_index_ < rhs.file_index_;
}

bool operator<=(const Archive::Iterator& lhs, const Archive::Iterator& rhs)
{
	return operator<(lhs, rhs) || operator==(lhs, rhs);
}

bool operator>(const Archive::Iterator& lhs, const Archive::Iterator& rhs)
{
	return !operator<=(lhs, rhs);
}

bool operator>=(const Archive::Iterator& lhs, const Archive::Iterator& rhs)
{
	return operator>(lhs, rhs) || operator==(lhs, rhs);
}

}  // namespace sarc
