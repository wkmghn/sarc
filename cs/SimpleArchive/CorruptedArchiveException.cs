//
// Copyright (c) 2016 wkmghn.
// 
// Use, modification and distribution is subject to the Boost Software License,
// Version 1.0. (See accompanying file LICENSE_1_0.txt or copy at
// http://www.boost.org/LICENSE_1_0.txt)
//
using System;

namespace SimpleArchive
{
    public class CorruptedArchiveException : Exception
    {
        public CorruptedArchiveException() : base() { }
        public CorruptedArchiveException(string message) : base(message) { }
        public CorruptedArchiveException(string message, Exception innerException) : base(message, innerException) { }
    }
}
