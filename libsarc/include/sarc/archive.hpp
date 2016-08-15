#pragma once

#include <cstdint>
#include <iterator>
#include <sarc/file_accessor.hpp>

namespace sarc
{

class Archive
{
public:

	class Iterator;

	/// アーカイブのパース結果を表します。
	/// Succeeded 以外はすべて失敗を表します。
	enum class ParsingResult
	{
		/// パースに成功しました。
		Succeeded,

		/// パースに失敗しました。
		/// パース対象の data が null です。
		NullData,

		/// パースに失敗しました。
		/// data_size がアーカイブの最小サイズ未満でした。
		TooFewDataSize,

		/// パースに失敗しました。
		/// データが壊れているか、アーカイブデータではありません。
		DataCorrupted,

		/// パースに失敗しました。
		/// 互換性のないバージョンのアーカイブデータです。
		UnsupportedVersion,
	};

	/// Archive インスタンスを初期化すると同時に data をパースします。
	/// パース結果は parsing_result で取得できます。
	Archive(const void* data, std::uint32_t data_size);

	~Archive();

	/// コピー/ムーブはできません。
	Archive(const Archive&) = delete;
	Archive& operator=(const Archive&) = delete;

	/// アーカイブのパース結果を取得します。
	/// Succeeded 以外の値の場合、アーカイブの内容にアクセスできません。
	ParsingResult parsing_result() const { return parsing_result_; }

	/// アーカイブに含まれるファイルの総数を取得します。
	/// アーカイブのパースに失敗している場合は常にゼロを返します。
	std::uint32_t num_files() const;

	/// アーカイブ内の file_index 番目のファイルを取得します。
	/// @param[in] file_index
	///		アーカイブ内のファイルを特定するためのインデックス。
	///		0 から num_files - 1 の範囲である必要があります。
	/// @return
	///		file_index 番目のファイルを返します。
	///		アーカイブのパースに失敗している場合は null を返します。
	///		file_index の値が範囲外の場合も null を返します。
	FileAccessor get_file(std::uint32_t file_index) const;
	
	/// アーカイブ内から指定の名前のファイルを検索します。
	/// 
	/// この関数によるファイル検索のコストは O(N) です。
	/// ここで N はアーカイブに含まれるファイル数です。。
	/// @return
	///		見つかったファイルを返します。
	///		以下の場合は無効な FileAccessor を返します。
	///		  * 指定の名前のファイルがアーカイブ内に存在しない場合
	///       * アーカイブのパースに失敗している場合
	///       * file_name が null の場合
	FileAccessor find_file(const char* file_name) const;

	/// アーカイブ内のすべてのファイルを列挙するためのイテレータを作成します。
	Iterator begin() const;

	/// アーカイブ内のすべてのファイルを列挙するためのイテレータを作成します。
	Iterator end() const;

private:

	ParsingResult parsing_result_;
	const std::uint8_t* data_;
	const std::uint32_t data_size_;

};

class Archive::Iterator
{
public:

	using iterator_category = std::random_access_iterator_tag;
	using value_type        = const FileAccessor;
	using difference_type   = std::int32_t;
	using pointer           = value_type*;
	using reference         = value_type;

	Iterator();
	Iterator(const Archive* archive, std::uint32_t file_index);
	Iterator(const Iterator&) = default;
	Iterator& operator=(const Iterator&) = default;
	~Iterator() = default;

	value_type operator*() const;
	value_type operator[](difference_type index) const;
	Iterator& operator++();
	Iterator operator++(int);
	Iterator& operator--();
	Iterator operator--(int);
	Iterator& operator+=(difference_type rhs);
	Iterator& operator-=(difference_type rhs);

	friend Iterator operator+(const Iterator& lhs, difference_type rhs);
	friend Iterator operator+(difference_type lhs, const Iterator& rhs);
	friend Iterator operator-(const Iterator& lhs, difference_type rhs);
	friend difference_type operator-(const Iterator& lhs, const Iterator& rhs);

	friend bool operator==(const Iterator& lhs, const Iterator& rhs);
	friend bool operator!=(const Iterator& lhs, const Iterator& rhs);
	friend bool operator<(const Iterator& lhs, const Iterator& rhs);
	friend bool operator<=(const Iterator& lhs, const Iterator& rhs);
	friend bool operator>(const Iterator& lhs, const Iterator& rhs);
	friend bool operator>=(const Iterator& lhs, const Iterator& rhs);

private:

	const Archive* archive_;
	std::uint32_t file_index_;

};

}  // namespace sarc
