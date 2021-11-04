// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

use crate::ffi::{DWORD, LPCVOID, LPCWSTR, REFCLSID, REFIID, ULONG};
use com::{interfaces::iunknown::IUnknown, sys::HRESULT};

interfaces! {
    /// Provides methods to create a new metadata scope, or open an existing one.
    #[uuid("809C652E-7396-11D2-9771-00A0C9B4D50C")]
    pub unsafe interface IMetaDataDispenser: IUnknown {
        /// Creates a new area in memory in which you can create new metadata.
        fn DefineScope(
            &self,
            rclsid: REFCLSID,                       // [in] What version to create.
            dwCreateFlags: DWORD,                   // [in] Flags on the create.
            riid: REFIID,                           // [in] The interface desired.
            ppIUnk: *mut *mut IUnknown) -> HRESULT; // [out] Return interface on success.

        /// Opens an existing, on-disk file and maps its metadata into memory.
        fn OpenScope(
            &self,
            szScope: LPCWSTR,                       // [in] The scope to open.
            dwOpenFlags: DWORD,                     // [in] Open mode flags.
            riid: REFIID,                           // [in] The interface desired.
            ppIUnk: *mut *mut IUnknown) -> HRESULT; // [out] Return interface on success.

        /// Opens an area of memory that contains existing metadata.
        /// That is, this method opens a specified area of memory in which the existing data is treated as metadata.
        fn OpenScopeOnMemory(
            &self,
            pData: LPCVOID,                         // [in] Location of scope data.
            cbData: ULONG,                          // [in] Size of the data pointed to by pData.
            dwOpenFlags: DWORD,                     // [in] Open mode flags.
            riid: REFIID,                           // [in] The interface desired.
            ppIUnk: *mut *mut IUnknown) -> HRESULT; // [out] Return interface on success.
    }
}
