// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

use crate::ffi::*;
use com::{interfaces::iunknown::IUnknown, sys::HRESULT};
use std::ffi::c_void;

interfaces! {
    #[uuid("0c733a30-2a1c-11ce-ade5-00aa0044773d")]
    pub unsafe interface ISequentialStream: IUnknown {
        pub unsafe fn Read(&self, pv: *mut c_void, cb: ULONG, pcbRead: *mut ULONG) -> HRESULT;
        pub unsafe fn Write(&self, pv: *const c_void, cb: ULONG, pcbWritten: *mut ULONG) -> HRESULT;
    }

    #[uuid("0000000c-0000-0000-C000-000000000046")]
    pub unsafe interface IStream: ISequentialStream {
        /// The Seek method changes the seek pointer to a new location. The new location is
        /// relative to either the beginning of the stream, the end of the stream, or the current seek pointer.
        pub unsafe fn Seek(&self,
            dlibMove: LARGE_INTEGER,
            dwOrigin: DWORD,
            plibNewPosition: *const ULARGE_INTEGER) -> HRESULT;

        pub unsafe fn SetSize(&self, libNewSize: ULARGE_INTEGER) -> HRESULT;

        pub unsafe fn CopyTo(&self,
            pstm: *const IStream,
            cb: ULARGE_INTEGER,
            pcbRead: *const ULARGE_INTEGER,
            pcbWritten: *const ULARGE_INTEGER) -> HRESULT;

        pub unsafe fn Commit(&self, grfCommitFlags: DWORD ) -> HRESULT;

        pub unsafe fn Revert(&self) -> HRESULT;

        pub unsafe fn LockRegion(&self,
            libOffset: ULARGE_INTEGER,
            cb: ULARGE_INTEGER,
            dwLockType: DWORD) -> HRESULT;

        pub unsafe fn UnlockRegion(&self,
            libOffset: ULARGE_INTEGER,
            cb: ULARGE_INTEGER,
            dwLockType: DWORD) -> HRESULT;

        /// The Stat method retrieves the STATSTG structure for this stream.
        pub unsafe fn Stat(&self, pstatstg: *const STATSTG, grfStatFlag: DWORD) -> HRESULT;

        /// The Clone method creates a new stream object with its own seek pointer that
        /// references the same bytes as the original stream.
        pub unsafe fn Clone(&self, ppstm: *const *const IStream) -> HRESULT;
    }
}
