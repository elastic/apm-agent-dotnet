#![allow(non_camel_case_types, non_snake_case, non_upper_case_globals)]
#![allow(overflowing_literals)]

// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// Copyright 2019 Camden Reslink
// MIT License
// https://github.com/camdenreslink/clr-profiler
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software
// and associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING
// BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

use com::{
    sys::{GUID, HRESULT},
    AbiTransferable, CLSID, IID,
};
use core::ffi::c_void;
use num_traits::FromPrimitive;
use std::{intrinsics::transmute, ptr};

// numeric types
pub type c_int = i32;
pub type c_long = i32;
pub type c_uint = u32;
pub type c_ulong = u32;
pub type c_ushort = u16;
pub type c_uchar = u8;
pub type int = c_int;
pub type BOOL = c_int;
pub type USHORT = c_ushort;
pub type UINT = c_uint;
pub type ULONG32 = c_uint;
pub type ULONG = c_ulong;
pub type DWORD = c_ulong;
pub type BYTE = c_uchar;
pub type COR_SIGNATURE = BYTE;

// TODO: LARGE_INTEGER and ULARGE_INTEGER need to be modelled as structs
pub type LARGE_INTEGER = i64;
pub type ULARGE_INTEGER = u64;

// char types
pub type wchar_t = u16;
pub type WCHAR = wchar_t;
pub type LPCWSTR = *const WCHAR;
pub type MDUTF8CSTR = *const c_uchar;
pub type LPOLESTR = LPCWSTR;

// pointer types
pub type UINT_PTR = usize;
pub type ULONG_PTR = usize;
pub type LPCBYTE = *const BYTE;
pub type SIZE_T = ULONG_PTR;
pub type LPVOID = *mut c_void;
pub type HANDLE = *mut c_void;
pub type UVCP_CONSTANT = *const c_void;
pub type LPCVOID = *const c_void;

// guid types
pub type REFGUID = *const GUID;
pub type REFCLSID = *const IID;
pub type REFIID = *const IID;

// profiler-specific pointers
pub type AppDomainID = UINT_PTR;
pub type AssemblyID = UINT_PTR;
pub type ClassID = UINT_PTR;
pub type ContextID = UINT_PTR;
pub type COR_PRF_ELT_INFO = UINT_PTR;
pub type COR_PRF_FRAME_INFO = UINT_PTR;
pub type FunctionID = UINT_PTR;
pub type GCHandleID = UINT_PTR;
pub type ModuleID = UINT_PTR;
pub type ObjectID = UINT_PTR;
pub type PCOR_SIGNATURE = *mut COR_SIGNATURE;
pub type PCCOR_SIGNATURE = *const COR_SIGNATURE;
pub type ProcessID = UINT_PTR;
pub type ReJITID = UINT_PTR;
pub type ThreadID = UINT_PTR;
pub type ClrInstanceID = USHORT;
pub type HCORENUM = *const c_void;
pub type RID = ULONG;

#[repr(C)]
pub union FunctionIDOrClientID {
    functionID: FunctionID,
    clientID: UINT_PTR,
}

// token types
pub type mdToken = ULONG32;
pub type mdModule = mdToken;
pub type mdTypeRef = mdToken;
pub type mdTypeDef = mdToken;
pub type mdFieldDef = mdToken;
pub type mdMethodDef = mdToken;
pub type mdParamDef = mdToken;
pub type mdInterfaceImpl = mdToken;
pub type mdMemberRef = mdToken;
pub type mdCustomAttribute = mdToken;
pub type mdPermission = mdToken;
pub type mdSignature = mdToken;
pub type mdEvent = mdToken;
pub type mdProperty = mdToken;
pub type mdModuleRef = mdToken;
pub type mdAssembly = mdToken;
pub type mdAssemblyRef = mdToken;
pub type mdFile = mdToken;
pub type mdExportedType = mdToken;
pub type mdManifestResource = mdToken;
pub type mdTypeSpec = mdToken;
pub type mdGenericParam = mdToken;
pub type mdMethodSpec = mdToken;
pub type mdGenericParamConstraint = mdToken;
pub type mdString = mdToken;
pub type mdCPToken = mdToken;

// nil tokens
pub const mdTokenNil: mdToken = 0;
pub const mdModuleNil: mdModule = CorTokenType::mdtModule.bits();
pub const mdTypeRefNil: mdTypeRef = CorTokenType::mdtTypeRef.bits();
pub const mdTypeDefNil: mdTypeDef = CorTokenType::mdtTypeDef.bits();
pub const mdFieldDefNil: mdFieldDef = CorTokenType::mdtFieldDef.bits();
pub const mdMethodDefNil: mdMethodDef = CorTokenType::mdtMethodDef.bits();
pub const mdParamDefNil: mdParamDef = CorTokenType::mdtParamDef.bits();
pub const mdInterfaceImplNil: mdInterfaceImpl = CorTokenType::mdtInterfaceImpl.bits();
pub const mdMemberRefNil: mdMemberRef = CorTokenType::mdtMemberRef.bits();
pub const mdCustomAttributeNil: mdCustomAttribute = CorTokenType::mdtCustomAttribute.bits();
pub const mdPermissionNil: mdPermission = CorTokenType::mdtPermission.bits();
pub const mdSignatureNil: mdSignature = CorTokenType::mdtSignature.bits();
pub const mdEventNil: mdEvent = CorTokenType::mdtEvent.bits();
pub const mdPropertyNil: mdProperty = CorTokenType::mdtProperty.bits();
pub const mdModuleRefNil: mdModuleRef = CorTokenType::mdtModuleRef.bits();
pub const mdTypeSpecNil: mdTypeSpec = CorTokenType::mdtTypeSpec.bits();
pub const mdAssemblyNil: mdAssembly = CorTokenType::mdtAssembly.bits();
pub const mdAssemblyRefNil: mdAssemblyRef = CorTokenType::mdtAssemblyRef.bits();
pub const mdFileNil: mdFile = CorTokenType::mdtFile.bits();
pub const mdExportedTypeNil: mdExportedType = CorTokenType::mdtExportedType.bits();
pub const mdManifestResourceNil: mdManifestResource = CorTokenType::mdtManifestResource.bits();
pub const mdGenericParamNil: mdGenericParam = CorTokenType::mdtGenericParam.bits();
pub const mdGenericParamConstraintNil: mdGenericParamConstraint =
    CorTokenType::mdtGenericParamConstraint.bits();
pub const mdMethodSpecNil: mdMethodSpec = CorTokenType::mdtMethodSpec.bits();
pub const mdStringNil: mdString = CorTokenType::mdtString.bits();

// function pointer types
pub type FunctionEnter = unsafe extern "system" fn(funcID: FunctionID) -> ();
pub type FunctionLeave = unsafe extern "system" fn(funcID: FunctionID) -> ();
pub type FunctionTailcall = unsafe extern "system" fn(funcID: FunctionID) -> ();
pub type FunctionIDMapper =
    unsafe extern "system" fn(funcId: FunctionID, pbHookFunction: *mut BOOL) -> UINT_PTR;
pub type FunctionEnter2 = unsafe extern "system" fn(
    funcId: FunctionID,
    clientData: UINT_PTR,
    func: COR_PRF_FRAME_INFO,
    argumentInfo: *const COR_PRF_FUNCTION_ARGUMENT_INFO,
) -> ();
pub type FunctionLeave2 = unsafe extern "system" fn(
    funcId: FunctionID,
    clientData: UINT_PTR,
    func: COR_PRF_FRAME_INFO,
    retvalRange: *const COR_PRF_FUNCTION_ARGUMENT_RANGE,
) -> ();
pub type FunctionTailcall2 = unsafe extern "system" fn(
    funcId: FunctionID,
    clientData: UINT_PTR,
    func: COR_PRF_FRAME_INFO,
) -> ();
pub type FunctionIDMapper2 = unsafe extern "system" fn(
    funcId: FunctionID,
    clientData: *const c_void,
    pbHookFunction: *mut BOOL,
) -> UINT_PTR;
pub type FunctionEnter3 =
    unsafe extern "system" fn(functionIDOrClientID: FunctionIDOrClientID) -> ();
pub type FunctionLeave3 =
    unsafe extern "system" fn(functionIDOrClientID: FunctionIDOrClientID) -> ();

pub type FunctionTailcall3 =
    unsafe extern "system" fn(functionIDOrClientID: FunctionIDOrClientID) -> ();

pub type FunctionEnter3WithInfo = unsafe extern "system" fn(
    functionIDOrClientID: FunctionIDOrClientID,
    eltInfo: COR_PRF_ELT_INFO,
) -> ();

pub type FunctionLeave3WithInfo = unsafe extern "system" fn(
    functionIDOrClientID: FunctionIDOrClientID,
    eltInfo: COR_PRF_ELT_INFO,
) -> ();

pub type FunctionTailcall3WithInfo = unsafe extern "system" fn(
    functionIDOrClientID: FunctionIDOrClientID,
    eltInfo: COR_PRF_ELT_INFO,
) -> ();
pub type StackSnapshotCallback = unsafe extern "system" fn(
    funcId: FunctionID,
    ip: UINT_PTR,
    frameInfo: COR_PRF_FRAME_INFO,
    contextSize: ULONG32,
    context: *const BYTE,
    clientData: *const c_void,
) -> HRESULT;
pub type ObjectReferenceCallback = unsafe extern "system" fn(
    root: ObjectID,
    reference: *const ObjectID,
    clientData: *const c_void,
) -> BOOL;

// profiler types
#[repr(C)]
#[derive(Debug, PartialEq, Clone, Copy)]
pub enum COR_PRF_JIT_CACHE {
    COR_PRF_CACHED_FUNCTION_FOUND = 0,
    COR_PRF_CACHED_FUNCTION_NOT_FOUND = 1,
}

#[repr(C)]
#[derive(Debug, PartialEq, Clone, Copy)]
pub enum COR_PRF_TRANSITION_REASON {
    COR_PRF_TRANSITION_CALL = 0,
    COR_PRF_TRANSITION_RETURN = 1,
}

#[repr(C)]
#[derive(Debug, PartialEq, Copy, Clone)]
pub enum COR_PRF_SUSPEND_REASON {
    COR_PRF_SUSPEND_OTHER = 0,
    COR_PRF_SUSPEND_FOR_GC = 1,
    COR_PRF_SUSPEND_FOR_APPDOMAIN_SHUTDOWN = 2,
    COR_PRF_SUSPEND_FOR_CODE_PITCHING = 3,
    COR_PRF_SUSPEND_FOR_SHUTDOWN = 4,
    COR_PRF_SUSPEND_FOR_INPROC_DEBUGGER = 6,
    COR_PRF_SUSPEND_FOR_GC_PREP = 7,
    COR_PRF_SUSPEND_FOR_REJIT = 8,
}
#[repr(C)]
#[derive(Debug, PartialEq, Copy, Clone)]
pub enum COR_PRF_GC_REASON {
    COR_PRF_GC_INDUCED = 1,
    COR_PRF_GC_OTHER = 0,
}
#[repr(C)]
#[derive(Debug, PartialEq)]
pub enum COR_PRF_GC_ROOT_KIND {
    COR_PRF_GC_ROOT_STACK = 1,
    COR_PRF_GC_ROOT_FINALIZER = 2,
    COR_PRF_GC_ROOT_HANDLE = 3,
    COR_PRF_GC_ROOT_OTHER = 0,
}
#[repr(C)]
#[derive(Debug, PartialEq)]
pub enum COR_PRF_GC_ROOT_FLAGS {
    COR_PRF_GC_ROOT_PINNING = 1,
    COR_PRF_GC_ROOT_WEAKREF = 2,
    COR_PRF_GC_ROOT_INTERIOR = 4,
    COR_PRF_GC_ROOT_REFCOUNTED = 8,
}
#[repr(C)]
#[derive(Debug, PartialEq)]
pub struct COR_IL_MAP {
    pub oldOffset: ULONG32,
    pub newOffset: ULONG32,
    pub fAccurate: BOOL,
}
#[repr(C)]
#[derive(Debug, PartialEq)]
pub struct COR_DEBUG_IL_TO_NATIVE_MAP {
    pub ilOffset: ULONG32,
    pub nativeStartOffset: ULONG32,
    pub nativeEndOffset: ULONG32,
}
#[repr(C)]
#[derive(Debug, PartialEq)]
pub struct COR_FIELD_OFFSET {
    pub ridOfField: mdFieldDef,
    pub ulOffset: ULONG,
}
#[repr(C)]
#[derive(Debug, PartialEq)]
pub struct COR_PRF_CODE_INFO {
    pub startAddress: UINT_PTR,
    pub size: SIZE_T,
}
#[repr(C)]
#[derive(Debug, PartialEq)]
pub enum COR_PRF_STATIC_TYPE {
    COR_PRF_FIELD_NOT_A_STATIC = 0,
    COR_PRF_FIELD_APP_DOMAIN_STATIC = 1,
    COR_PRF_FIELD_THREAD_STATIC = 2,
    COR_PRF_FIELD_CONTEXT_STATIC = 4,
    COR_PRF_FIELD_RVA_STATIC = 8,
}
#[repr(C)]
#[derive(Debug, PartialEq)]
pub enum COR_PRF_GC_GENERATION {
    COR_PRF_GC_GEN_0 = 0,
    COR_PRF_GC_GEN_1 = 1,
    COR_PRF_GC_GEN_2 = 2,
    COR_PRF_GC_LARGE_OBJECT_HEAP = 3,
}
#[repr(C)]
#[derive(Debug, PartialEq)]
pub struct COR_PRF_GC_GENERATION_RANGE {
    pub generation: COR_PRF_GC_GENERATION,
    pub rangeStart: ObjectID,
    pub rangeLength: UINT_PTR,
    pub rangeLengthReserved: UINT_PTR,
}
#[repr(C)]
#[derive(Debug, PartialEq)]
pub enum COR_PRF_CLAUSE_TYPE {
    COR_PRF_CLAUSE_NONE = 0,
    COR_PRF_CLAUSE_FILTER = 1,
    COR_PRF_CLAUSE_CATCH = 2,
    COR_PRF_CLAUSE_FINALLY = 3,
}
#[repr(C)]
#[derive(Debug, PartialEq)]
pub struct COR_PRF_EX_CLAUSE_INFO {
    pub clauseType: COR_PRF_CLAUSE_TYPE,
    pub programCounter: UINT_PTR,
    pub framePointer: UINT_PTR,
    pub shadowStackPointer: UINT_PTR,
}
#[repr(C)]
#[derive(Debug, PartialEq)]
pub struct COR_PRF_FUNCTION_ARGUMENT_RANGE {
    pub startAddress: UINT_PTR,
    pub length: ULONG,
}
#[repr(C)]
#[derive(Debug, PartialEq)]
pub struct COR_PRF_FUNCTION_ARGUMENT_INFO {
    pub numRanges: ULONG,
    pub totalArgumentSize: ULONG,
    pub ranges: [COR_PRF_FUNCTION_ARGUMENT_RANGE; 1],
}
#[repr(C)]
#[derive(Debug, PartialEq)]
pub enum COR_PRF_RUNTIME_TYPE {
    COR_PRF_DESKTOP_CLR = 1,
    COR_PRF_CORE_CLR = 2,
}
#[repr(C)]
#[derive(Debug, PartialEq, FromPrimitive)]
pub enum CorElementType {
    ELEMENT_TYPE_END = 0x00,
    ELEMENT_TYPE_VOID = 0x01,
    ELEMENT_TYPE_BOOLEAN = 0x02,
    ELEMENT_TYPE_CHAR = 0x03,
    ELEMENT_TYPE_I1 = 0x04,
    ELEMENT_TYPE_U1 = 0x05,
    ELEMENT_TYPE_I2 = 0x06,
    ELEMENT_TYPE_U2 = 0x07,
    ELEMENT_TYPE_I4 = 0x08,
    ELEMENT_TYPE_U4 = 0x09,
    ELEMENT_TYPE_I8 = 0x0a,
    ELEMENT_TYPE_U8 = 0x0b,
    ELEMENT_TYPE_R4 = 0x0c,
    ELEMENT_TYPE_R8 = 0x0d,
    ELEMENT_TYPE_STRING = 0x0e,

    // every type above PTR will be simple type
    ELEMENT_TYPE_PTR = 0x0f,   // PTR <type>
    ELEMENT_TYPE_BYREF = 0x10, // BYREF <type>

    // Please use ELEMENT_TYPE_VALUETYPE. ELEMENT_TYPE_VALUECLASS is deprecated.
    ELEMENT_TYPE_VALUETYPE = 0x11,   // VALUETYPE <class Token>
    ELEMENT_TYPE_CLASS = 0x12,       // CLASS <class Token>
    ELEMENT_TYPE_VAR = 0x13,         // a class type variable VAR <number>
    ELEMENT_TYPE_ARRAY = 0x14, // MDARRAY <type> <rank> <bcount> <bound1> ... <lbcount> <lb1> ...
    ELEMENT_TYPE_GENERICINST = 0x15, // GENERICINST <generic type> <argCnt> <arg1> ... <argn>
    ELEMENT_TYPE_TYPEDBYREF = 0x16, // TYPEDREF  (it takes no args) a typed referece to some other type

    ELEMENT_TYPE_I = 0x18,       // native integer size
    ELEMENT_TYPE_U = 0x19,       // native unsigned integer size
    ELEMENT_TYPE_FNPTR = 0x1b, // FNPTR <complete sig for the function including calling convention>
    ELEMENT_TYPE_OBJECT = 0x1c, // Shortcut for System.Object
    ELEMENT_TYPE_SZARRAY = 0x1d, // Shortcut for single dimension zero lower bound array
    // SZARRAY <type>
    ELEMENT_TYPE_MVAR = 0x1e, // a method type variable MVAR <number>

    // This is only for binding
    ELEMENT_TYPE_CMOD_REQD = 0x1f, // required C modifier : E_T_CMOD_REQD <mdTypeRef/mdTypeDef>
    ELEMENT_TYPE_CMOD_OPT = 0x20,  // optional C modifier : E_T_CMOD_OPT <mdTypeRef/mdTypeDef>

    // This is for signatures generated internally (which will not be persisted in any way).
    ELEMENT_TYPE_INTERNAL = 0x21, // INTERNAL <typehandle>

    // Note that this is the max of base type excluding modifiers
    ELEMENT_TYPE_MAX = 0x22, // first invalid element type

    ELEMENT_TYPE_MODIFIER = 0x40,
    ELEMENT_TYPE_SENTINEL = 0x01 | CorElementType::ELEMENT_TYPE_MODIFIER as isize, // sentinel for varargs
    ELEMENT_TYPE_PINNED = 0x05 | CorElementType::ELEMENT_TYPE_MODIFIER as isize,
}
impl From<DWORD> for CorElementType {
    fn from(d: DWORD) -> Self {
        unsafe { transmute(d as DWORD) }
    }
}
#[repr(C)]
#[derive(Debug, PartialEq, Default)]
pub struct OSINFO {
    dwOSPlatformId: DWORD,   // Operating system platform.
    dwOSMajorVersion: DWORD, // OS Major version.
    dwOSMinorVersion: DWORD, // OS Minor version.
}
/// Managed assembly metadata
#[repr(C)]
#[derive(Clone, Debug, PartialEq)]
pub struct ASSEMBLYMETADATA {
    /// Major version
    pub usMajorVersion: USHORT,
    /// Minor version
    pub usMinorVersion: USHORT,
    /// Build number
    pub usBuildNumber: USHORT,
    /// Revision number
    pub usRevisionNumber: USHORT,
    /// Locale
    pub szLocale: *mut WCHAR,
    /// Locale buffer size in wide chars
    pub cbLocale: ULONG,
    /// Processor ID array
    pub rProcessor: *mut DWORD,
    /// Processor ID array size
    pub ulProcessor: ULONG,
    /// OS info array
    pub rOS: *mut OSINFO,
    /// OS info array size
    pub ulOS: ULONG,
}
impl Default for ASSEMBLYMETADATA {
    fn default() -> Self {
        Self {
            usMajorVersion: 0,
            usMinorVersion: 0,
            usBuildNumber: 0,
            usRevisionNumber: 0,
            szLocale: ptr::null_mut(),
            cbLocale: 0,
            rProcessor: ptr::null_mut(),
            ulProcessor: 0,
            rOS: ptr::null_mut(),
            ulOS: 0,
        }
    }
}

#[repr(C)]
#[derive(Debug, PartialEq)]
pub struct FILETIME {
    dwLowDateTime: DWORD,
    dwHighDateTime: DWORD,
}
#[repr(C)]
#[derive(Debug, PartialEq)]
pub struct STATSTG {
    pwcsName: LPOLESTR,
    r#type: DWORD,
    cbSize: ULARGE_INTEGER,
    mtime: FILETIME,
    ctime: FILETIME,
    atime: FILETIME,
    grfMode: DWORD,
    grfLocksSupported: DWORD,
    clsid: CLSID,
    grfStateBits: DWORD,
    reserved: DWORD,
}

#[repr(C)]
#[derive(Debug, PartialEq)]
pub struct COR_PRF_ASSEMBLY_REFERENCE_INFO {
    pub pbPublicKeyOrToken: *const c_void,
    pub cbPublicKeyOrToken: ULONG,
    pub szName: LPCWSTR,
    pub pMetaData: *const ASSEMBLYMETADATA,
    pub pbHashValue: *const c_void,
    pub cbHashValue: ULONG,
    pub dwAssemblyRefFlags: DWORD,
}
#[repr(C)]
#[derive(Debug, PartialEq)]
pub struct COR_PRF_FUNCTION {
    functionId: FunctionID,
    reJitId: ReJITID,
}
bitflags! {
    pub struct COR_PRF_MONITOR: DWORD {
        const COR_PRF_MONITOR_NONE = 0;
        const COR_PRF_MONITOR_FUNCTION_UNLOADS = 0x1;
        const COR_PRF_MONITOR_CLASS_LOADS = 0x2;
        const COR_PRF_MONITOR_MODULE_LOADS = 0x4;
        const COR_PRF_MONITOR_ASSEMBLY_LOADS = 0x8;
        const COR_PRF_MONITOR_APPDOMAIN_LOADS = 0x10;
        const COR_PRF_MONITOR_JIT_COMPILATION = 0x20;
        const COR_PRF_MONITOR_EXCEPTIONS = 0x40;
        const COR_PRF_MONITOR_GC = 0x80;
        const COR_PRF_MONITOR_OBJECT_ALLOCATED = 0x100;
        const COR_PRF_MONITOR_THREADS = 0x200;
        const COR_PRF_MONITOR_REMOTING = 0x400;
        const COR_PRF_MONITOR_CODE_TRANSITIONS = 0x800;
        const COR_PRF_MONITOR_ENTERLEAVE = 0x1000;
        const COR_PRF_MONITOR_CCW = 0x2000;
        const COR_PRF_MONITOR_REMOTING_COOKIE = 0x4000 | Self::COR_PRF_MONITOR_REMOTING.bits;
        const COR_PRF_MONITOR_REMOTING_ASYNC = 0x8000 | Self::COR_PRF_MONITOR_REMOTING.bits;
        const COR_PRF_MONITOR_SUSPENDS = 0x10000;
        const COR_PRF_MONITOR_CACHE_SEARCHES = 0x20000;
        const COR_PRF_ENABLE_REJIT = 0x40000;
        const COR_PRF_ENABLE_INPROC_DEBUGGING = 0x80000;
        const COR_PRF_ENABLE_JIT_MAPS = 0x100000;
        const COR_PRF_DISABLE_INLINING = 0x200000;
        const COR_PRF_DISABLE_OPTIMIZATIONS = 0x400000;
        const COR_PRF_ENABLE_OBJECT_ALLOCATED = 0x800000;
        const COR_PRF_MONITOR_CLR_EXCEPTIONS = 0x1000000;
        const COR_PRF_MONITOR_ALL = 0x107ffff;
        const COR_PRF_ENABLE_FUNCTION_ARGS = 0x2000000;
        const COR_PRF_ENABLE_FUNCTION_RETVAL = 0x4000000;
        const COR_PRF_ENABLE_FRAME_INFO = 0x8000000;
        const COR_PRF_ENABLE_STACK_SNAPSHOT = 0x10000000;
        const COR_PRF_USE_PROFILE_IMAGES = 0x20000000;
        const COR_PRF_DISABLE_TRANSPARENCY_CHECKS_UNDER_FULL_TRUST = 0x40000000;
        const COR_PRF_DISABLE_ALL_NGEN_IMAGES = 0x80000000;
        const COR_PRF_ALL = 0x8fffffff;
        const COR_PRF_REQUIRE_PROFILE_IMAGE = Self::COR_PRF_USE_PROFILE_IMAGES.bits
            | Self::COR_PRF_MONITOR_CODE_TRANSITIONS.bits
            | Self::COR_PRF_MONITOR_ENTERLEAVE.bits;
        const COR_PRF_ALLOWABLE_AFTER_ATTACH = Self::COR_PRF_MONITOR_THREADS.bits
            | Self::COR_PRF_MONITOR_MODULE_LOADS.bits
            | Self::COR_PRF_MONITOR_ASSEMBLY_LOADS.bits
            | Self::COR_PRF_MONITOR_APPDOMAIN_LOADS.bits
            | Self::COR_PRF_ENABLE_STACK_SNAPSHOT.bits
            | Self::COR_PRF_MONITOR_GC.bits
            | Self::COR_PRF_MONITOR_SUSPENDS.bits
            | Self::COR_PRF_MONITOR_CLASS_LOADS.bits
            | Self::COR_PRF_MONITOR_EXCEPTIONS.bits
            | Self::COR_PRF_MONITOR_JIT_COMPILATION.bits
            | Self::COR_PRF_ENABLE_REJIT.bits;
        const COR_PRF_MONITOR_IMMUTABLE = Self::COR_PRF_MONITOR_CODE_TRANSITIONS.bits
            | Self::COR_PRF_MONITOR_REMOTING.bits
            | Self::COR_PRF_MONITOR_REMOTING_COOKIE.bits
            | Self::COR_PRF_MONITOR_REMOTING_ASYNC.bits
            | Self::COR_PRF_ENABLE_INPROC_DEBUGGING.bits
            | Self::COR_PRF_ENABLE_JIT_MAPS.bits
            | Self::COR_PRF_DISABLE_OPTIMIZATIONS.bits
            | Self::COR_PRF_DISABLE_INLINING.bits
            | Self::COR_PRF_ENABLE_OBJECT_ALLOCATED.bits
            | Self::COR_PRF_ENABLE_FUNCTION_ARGS.bits
            | Self::COR_PRF_ENABLE_FUNCTION_RETVAL.bits
            | Self::COR_PRF_ENABLE_FRAME_INFO.bits
            | Self::COR_PRF_USE_PROFILE_IMAGES.bits
            | Self::COR_PRF_DISABLE_TRANSPARENCY_CHECKS_UNDER_FULL_TRUST.bits
            | Self::COR_PRF_DISABLE_ALL_NGEN_IMAGES.bits;
    }
}

bitflags! {
    pub struct COR_PRF_HIGH_MONITOR: DWORD {
        const COR_PRF_HIGH_MONITOR_NONE = 0;
        const COR_PRF_HIGH_ADD_ASSEMBLY_REFERENCES = 0x1;
        const COR_PRF_HIGH_IN_MEMORY_SYMBOLS_UPDATED = 0x2;
        const COR_PRF_HIGH_MONITOR_DYNAMIC_FUNCTION_UNLOADS = 0x4;
        const COR_PRF_HIGH_DISABLE_TIERED_COMPILATION = 0x8;
        const COR_PRF_HIGH_BASIC_GC = 0x10;
        const COR_PRF_HIGH_MONITOR_GC_MOVED_OBJECTS = 0x20;
        const COR_PRF_HIGH_REQUIRE_PROFILE_IMAGE = 0;
        const COR_PRF_HIGH_MONITOR_LARGEOBJECT_ALLOCATED = 0x40;
        const COR_PRF_HIGH_ALLOWABLE_AFTER_ATTACH = Self::COR_PRF_HIGH_IN_MEMORY_SYMBOLS_UPDATED.bits
            | Self::COR_PRF_HIGH_MONITOR_DYNAMIC_FUNCTION_UNLOADS.bits
            | Self::COR_PRF_HIGH_BASIC_GC.bits
            | Self::COR_PRF_HIGH_MONITOR_GC_MOVED_OBJECTS.bits
            | Self::COR_PRF_HIGH_MONITOR_LARGEOBJECT_ALLOCATED.bits;
        const COR_PRF_HIGH_MONITOR_IMMUTABLE = COR_PRF_HIGH_MONITOR::COR_PRF_HIGH_DISABLE_TIERED_COMPILATION.bits;
    }
}

#[repr(C)]
#[derive(Debug, PartialEq)]
pub struct COR_PRF_METHOD {
    moduleId: ModuleID,
    methodId: mdMethodDef,
}
bitflags! {
    pub struct CorOpenFlags: DWORD {
        const ofRead = 0x00000000;
        const ofWrite = 0x00000001;
        const ofReadWriteMask = 0x00000001;
        const ofCopyMemory = 0x00000002;
        const ofCacheImage = 0x00000004;
        const ofManifestMetadata = 0x00000008;
        const ofReadOnly = 0x00000010;
        const ofTakeOwnership = 0x00000020;
        const ofNoTypeLib = 0x00000080;
        const ofNoTransform = 0x00001000;
        const ofReserved1 = 0x00000100;
        const ofReserved2 = 0x00000200;
        const ofReserved = 0xffffff40;
    }
}

#[repr(C)]
#[derive(Debug, PartialEq)]
pub enum COR_PRF_SNAPSHOT_INFO {
    COR_PRF_SNAPSHOT_DEFAULT = 0x0,
    COR_PRF_SNAPSHOT_REGISTER_CONTEXT = 0x1,
    COR_PRF_SNAPSHOT_X86_OPTIMIZED = 0x2,
}
bitflags! {
    pub struct COR_PRF_MODULE_FLAGS: DWORD {
        const COR_PRF_MODULE_DISK = 0x1;
        const COR_PRF_MODULE_NGEN = 0x2;
        const COR_PRF_MODULE_DYNAMIC = 0x4;
        const COR_PRF_MODULE_COLLECTIBLE = 0x8;
        const COR_PRF_MODULE_RESOURCE = 0x10;
        const COR_PRF_MODULE_FLAT_LAYOUT = 0x20;
        const COR_PRF_MODULE_WINDOWS_RUNTIME = 0x40;
    }
}
bitflags! {
    pub struct COR_PRF_REJIT_FLAGS: DWORD {
        const COR_PRF_REJIT_BLOCK_INLINING = 0x1;
        const COR_PRF_REJIT_INLINING_CALLBACKS = 0x2;
    }
}
bitflags! {
    pub struct COR_PRF_FINALIZER_FLAGS: DWORD {
        const COR_PRF_FINALIZER_CRITICAL = 0x1;
    }
}
#[repr(C)]
#[derive(Debug, PartialEq, Clone, Copy)]
pub enum CorSaveSize {
    cssAccurate = 0x0000,            // Find exact save size, accurate but slower.
    cssQuick = 0x0001,               // Estimate save size, may pad estimate, but faster.
    cssDiscardTransientCAs = 0x0002, // remove all of the CAs of discardable types
}
#[repr(C)]
#[derive(Debug, PartialEq)]
pub struct COR_SECATTR {
    tkCtor: mdMemberRef,             // Ref to constructor of security attribute.
    pCustomAttribute: *const c_void, // Blob describing ctor args and field/property values.
    cbCustomAttribute: ULONG,        // Length of the above blob.
}
bitflags! {
    pub struct CorFieldAttr: DWORD {
        // member access mask - Use this mask to retrieve accessibility information.
        const fdFieldAccessMask           =   0x0007;
        const fdPrivateScope              =   0x0000;     // Member not referenceable.
        const fdPrivate                   =   0x0001;     // Accessible only by the parent type.
        const fdFamANDAssem               =   0x0002;     // Accessible by sub-types only in this Assembly.
        const fdAssembly                  =   0x0003;     // Accessibly by anyone in the Assembly.
        const fdFamily                    =   0x0004;     // Accessible only by type and sub-types.
        const fdFamORAssem                =   0x0005;     // Accessibly by sub-types anywhere, plus anyone in assembly.
        const fdPublic                    =   0x0006;     // Accessibly by anyone who has visibility to this scope.
        // end member access mask

        // field contract attributes.
        const fdStatic                    =   0x0010;     // Defined on type, else per instance.
        const fdInitOnly                  =   0x0020;     // Field may only be initialized, not written to after init.
        const fdLiteral                   =   0x0040;     // Value is compile time constant.
        const fdNotSerialized             =   0x0080;     // Field does not have to be serialized when type is remoted.

        const fdSpecialName               =   0x0200;     // field is special.  Name describes how.

        // interop attributes
        const fdPinvokeImpl               =   0x2000;     // Implementation is forwarded through pinvoke.

        // Reserved flags for runtime use only.
        const fdReservedMask              =   0x9500;
        const fdRTSpecialName             =   0x0400;     // Runtime(metadata internal APIs) should check name encoding.
        const fdHasFieldMarshal           =   0x1000;     // Field has marshalling information.
        const fdHasDefault                =   0x8000;     // Field has default.
        const fdHasFieldRVA               =   0x0100;     // Field has RVA.
    }
}
bitflags! {
    pub struct CorMethodAttr: DWORD {
        // member access mask - Use this mask to retrieve accessibility information.
        const mdMemberAccessMask          =   0x0007;
        const mdPrivateScope              =   0x0000;     // Member not referenceable.
        const mdPrivate                   =   0x0001;     // Accessible only by the parent type.
        const mdFamANDAssem               =   0x0002;     // Accessible by sub-types only in this Assembly.
        const mdAssem                     =   0x0003;     // Accessibly by anyone in the Assembly.
        const mdFamily                    =   0x0004;     // Accessible only by type and sub-types.
        const mdFamORAssem                =   0x0005;     // Accessibly by sub-types anywhere, plus anyone in assembly.
        const mdPublic                    =   0x0006;     // Accessibly by anyone who has visibility to this scope.
        // end member access mask

        // method contract attributes.
        const mdStatic                    =   0x0010;     // Defined on type, else per instance.
        const mdFinal                     =   0x0020;     // Method may not be overridden.
        const mdVirtual                   =   0x0040;     // Method virtual.
        const mdHideBySig                 =   0x0080;     // Method hides by name+sig, else just by name.

        // vtable layout mask - Use this mask to retrieve vtable attributes.
        const mdVtableLayoutMask          =   0x0100;
        const mdReuseSlot                 =   0x0000;     // The default.
        const mdNewSlot                   =   0x0100;     // Method always gets a new slot in the vtable.
        // end vtable layout mask

        // method implementation attributes.
        const mdCheckAccessOnOverride     =   0x0200;     // Overridability is the same as the visibility.
        const mdAbstract                  =   0x0400;     // Method does not provide an implementation.
        const mdSpecialName               =   0x0800;     // Method is special.  Name describes how.

        // interop attributes
        const mdPinvokeImpl               =   0x2000;     // Implementation is forwarded through pinvoke.
        const mdUnmanagedExport           =   0x0008;     // Managed method exported via thunk to unmanaged code.

        // Reserved flags for runtime use only.
        const mdReservedMask              =   0xd000;
        const mdRTSpecialName             =   0x1000;     // Runtime should check name encoding.
        const mdHasSecurity               =   0x4000;     // Method has security associate with it.
        const mdRequireSecObject          =   0x8000;     // Method calls another method containing security code.
    }
}
bitflags! {
    pub struct CorMethodImpl: DWORD {
        // code impl mask
        const miCodeTypeMask       =   0x0003;   // Flags about code type.
        const miIL                 =   0x0000;   // Method impl is IL.
        const miNative             =   0x0001;   // Method impl is native.
        const miOPTIL              =   0x0002;   // Method impl is OPTIL
        const miRuntime            =   0x0003;   // Method impl is provided by the runtime.
        // end code impl mask

        // managed mask
        const miManagedMask        =   0x0004;   // Flags specifying whether the code is managed or unmanaged.
        const miUnmanaged          =   0x0004;   // Method impl is unmanaged, otherwise managed.
        const miManaged            =   0x0000;   // Method impl is managed.
        // end managed mask

        // implementation info and interop
        const miForwardRef         =   0x0010;   // Indicates method is defined; used primarily in merge scenarios.
        const miPreserveSig        =   0x0080;   // Indicates method sig is not to be mangled to do HRESULT conversion.

        const miInternalCall       =   0x1000;   // Reserved for internal use.

        const miSynchronized       =   0x0020;   // Method is single threaded through the body.
        const miNoInlining         =   0x0008;   // Method may not be inlined.
        const miAggressiveInlining =   0x0100;   // Method should be inlined if possible.
        const miNoOptimization     =   0x0040;   // Method may not be optimized.
        const miAggressiveOptimization = 0x0200; // Method may contain hot code and should be aggressively optimized.

        // These are the flags that are allowed in MethodImplAttribute's Value
        // property. This should include everything above except the code impl
        // flags (which are used for MethodImplAttribute's MethodCodeType field).
        const miUserMask = Self::miManagedMask.bits
            | Self::miForwardRef.bits
            | Self::miPreserveSig.bits
            | Self::miInternalCall.bits
            | Self::miSynchronized.bits
            | Self::miNoInlining.bits
            | Self::miAggressiveInlining.bits
            | Self::miNoOptimization.bits
            | Self::miAggressiveOptimization.bits;

        const miMaxMethodImplVal   =   0xffff;   // Range check value
    }
}
bitflags! {
    pub struct CorPinvokeMap: DWORD {
        const pmNoMangle          = 0x0001;   // Pinvoke is to use the member name as specified.

        // Use this mask to retrieve the CharSet information.
        const pmCharSetMask       = 0x0006;
        const pmCharSetNotSpec    = 0x0000;
        const pmCharSetAnsi       = 0x0002;
        const pmCharSetUnicode    = 0x0004;
        const pmCharSetAuto       = 0x0006;

        const pmBestFitUseAssem   = 0x0000;
        const pmBestFitEnabled    = 0x0010;
        const pmBestFitDisabled   = 0x0020;
        const pmBestFitMask       = 0x0030;

        const pmThrowOnUnmappableCharUseAssem   = 0x0000;
        const pmThrowOnUnmappableCharEnabled    = 0x1000;
        const pmThrowOnUnmappableCharDisabled   = 0x2000;
        const pmThrowOnUnmappableCharMask       = 0x3000;

        const pmSupportsLastError = 0x0040;   // Information about target function. Not relevant for fields.

        // None of the calling convention flags is relevant for fields.
        const pmCallConvMask      = 0x0700;
        const pmCallConvWinapi    = 0x0100;   // Pinvoke will use native callconv appropriate to target windows platform.
        const pmCallConvCdecl     = 0x0200;
        const pmCallConvStdcall   = 0x0300;
        const pmCallConvThiscall  = 0x0400;   // In M9, pinvoke will raise exception.
        const pmCallConvFastcall  = 0x0500;

        const pmMaxValue          = 0xFFFF;
    }
}

pub const E_NOINTERFACE: HRESULT = 0x8000_4002;
pub const E_OUTOFMEMORY: HRESULT = 0x8007_000E;
pub const CLASS_E_NOAGGREGATION: HRESULT = 0x8004_0110;
pub const E_FAIL: HRESULT = 0x8000_4005;
pub const COR_E_INVALIDPROGRAM: HRESULT = 0x8013_153A;
pub const COR_E_INVALIDOPERATION: HRESULT = 0x8013_1509;
pub const COR_E_INDEXOUTOFRANGE: HRESULT = 0x8;

/// record not found on lookup
pub const CLDB_E_RECORD_NOTFOUND: HRESULT = 0x80131130;

bitflags! {
    pub struct CorAssemblyFlags: DWORD {
        const afPublicKey             =   0x0001;
        const afPA_None               =   0x0000;
        const afPA_MSIL               =   0x0010;
        const afPA_x86                =   0x0020;
        const afPA_IA64               =   0x0030;
        const afPA_AMD64              =   0x0040;
        const afPA_ARM                =   0x0050;
        const afPA_NoPlatform         =   0x0070;
        const afPA_Specified          =   0x0080;
        const afPA_Mask               =   0x0070;
        const afPA_FullMask           =   0x00F0;
        const afPA_Shift              =   0x0004;

        const afEnableJITcompileTracking  =   0x8000;
        const afDisableJITcompileOptimizer=   0x4000;

        const afRetargetable          =   0x0100;
        const afContentType_Default        =   0x0000;
        const afContentType_WindowsRuntime =   0x0200;
        const afContentType_Mask           =   0x0E00;
    }
}

bitflags! {
    /// Contains values that indicate type metadata.
    pub struct CorTypeAttr: DWORD {
        /// Used for type visibility information.
        const tdVisibilityMask        =   0x00000007;
        /// Specifies that the type is not in public scope.
        const tdNotPublic             =   0x00000000;
        /// Specifies that the type is in public scope.
        const tdPublic                =   0x00000001;
        /// Specifies that the type is nested with public visibility.
        const tdNestedPublic          =   0x00000002;
        /// Specifies that the type is nested with private visibility.
        const tdNestedPrivate         =   0x00000003;
        const tdNestedFamily          =   0x00000004;
        const tdNestedAssembly        =   0x00000005;
        const tdNestedFamANDAssem     =   0x00000006;
        const tdNestedFamORAssem      =   0x00000007;

        const tdLayoutMask            =   0x00000018;
        const tdAutoLayout            =   0x00000000;
        const tdSequentialLayout      =   0x00000008;
        const tdExplicitLayout        =   0x00000010;

        const tdClassSemanticsMask    =   0x00000020;
        const tdClass                 =   0x00000000;
        const tdInterface             =   0x00000020;

        const tdAbstract              =   0x00000080;
        const tdSealed                =   0x00000100;
        const tdSpecialName           =   0x00000400;

        const tdImport                =   0x00001000;
        const tdSerializable          =   0x00002000;
        const tdWindowsRuntime        =   0x00004000;

        const tdStringFormatMask      =   0x00030000;
        const tdAnsiClass             =   0x00000000;
        const tdUnicodeClass          =   0x00010000;
        const tdAutoClass             =   0x00020000;
        const tdCustomFormatClass     =   0x00030000;
        const tdCustomFormatMask      =   0x00C00000;

        const tdBeforeFieldInit       =   0x00100000;
        const tdForwarder             =   0x00200000;

        const tdReservedMask          =   0x00040800;
        const tdRTSpecialName         =   0x00000800;
        const tdHasSecurity           =   0x00040000;
    }
}

impl CorTypeAttr {
    pub fn IsTdNotPublic(x: CorTypeAttr) -> bool {
        x & CorTypeAttr::tdVisibilityMask == CorTypeAttr::tdNotPublic
    }
    pub fn IsTdPublic(x: CorTypeAttr) -> bool {
        x & CorTypeAttr::tdVisibilityMask == CorTypeAttr::tdPublic
    }
    pub fn IsTdNestedPublic(x: CorTypeAttr) -> bool {
        x & CorTypeAttr::tdVisibilityMask == CorTypeAttr::tdNestedPublic
    }
    pub fn IsTdNestedPrivate(x: CorTypeAttr) -> bool {
        x & CorTypeAttr::tdVisibilityMask == CorTypeAttr::tdNestedPrivate
    }
    pub fn IsTdNestedFamily(x: CorTypeAttr) -> bool {
        x & CorTypeAttr::tdVisibilityMask == CorTypeAttr::tdNestedFamily
    }
    pub fn IsTdNestedAssembly(x: CorTypeAttr) -> bool {
        x & CorTypeAttr::tdVisibilityMask == CorTypeAttr::tdNestedAssembly
    }
    pub fn IsTdNestedFamANDAssem(x: CorTypeAttr) -> bool {
        x & CorTypeAttr::tdVisibilityMask == CorTypeAttr::tdNestedFamANDAssem
    }
    pub fn IsTdNestedFamORAssem(x: CorTypeAttr) -> bool {
        x & CorTypeAttr::tdVisibilityMask == CorTypeAttr::tdNestedFamORAssem
    }
    pub fn IsTdNested(x: CorTypeAttr) -> bool {
        x & CorTypeAttr::tdVisibilityMask >= CorTypeAttr::tdNestedPublic
    }
    pub fn IsTdAutoLayout(x: CorTypeAttr) -> bool {
        x & CorTypeAttr::tdLayoutMask == CorTypeAttr::tdAutoLayout
    }
    pub fn IsTdSequentialLayout(x: CorTypeAttr) -> bool {
        x & CorTypeAttr::tdLayoutMask == CorTypeAttr::tdSequentialLayout
    }
    pub fn IsTdExplicitLayout(x: CorTypeAttr) -> bool {
        x & CorTypeAttr::tdLayoutMask == CorTypeAttr::tdExplicitLayout
    }

    pub fn IsTdClass(x: CorTypeAttr) -> bool {
        x & CorTypeAttr::tdClassSemanticsMask == CorTypeAttr::tdClass
    }
    pub fn IsTdInterface(x: CorTypeAttr) -> bool {
        x & CorTypeAttr::tdClassSemanticsMask == CorTypeAttr::tdInterface
    }

    pub fn IsTdAbstract(x: CorTypeAttr) -> bool {
        x.contains(CorTypeAttr::tdAbstract)
    }
    pub fn IsTdSealed(x: CorTypeAttr) -> bool {
        x.contains(CorTypeAttr::tdSealed)
    }
    pub fn IsTdSpecialName(x: CorTypeAttr) -> bool {
        x.contains(CorTypeAttr::tdSpecialName)
    }

    pub fn IsTdImport(x: CorTypeAttr) -> bool {
        x.contains(CorTypeAttr::tdImport)
    }
    pub fn IsTdSerializable(x: CorTypeAttr) -> bool {
        x.contains(CorTypeAttr::tdSerializable)
    }
    pub fn IsTdWindowsRuntime(x: CorTypeAttr) -> bool {
        x.contains(CorTypeAttr::tdWindowsRuntime)
    }

    pub fn IsTdAnsiClass(x: CorTypeAttr) -> bool {
        x & CorTypeAttr::tdStringFormatMask == CorTypeAttr::tdAnsiClass
    }
    pub fn IsTdUnicodeClass(x: CorTypeAttr) -> bool {
        x & CorTypeAttr::tdStringFormatMask == CorTypeAttr::tdUnicodeClass
    }
    pub fn IsTdAutoClass(x: CorTypeAttr) -> bool {
        x & CorTypeAttr::tdStringFormatMask == CorTypeAttr::tdAutoClass
    }
    pub fn IsTdCustomFormatClass(x: CorTypeAttr) -> bool {
        x & CorTypeAttr::tdStringFormatMask == CorTypeAttr::tdCustomFormatClass
    }
    pub fn IsTdBeforeFieldInit(x: CorTypeAttr) -> bool {
        x.contains(CorTypeAttr::tdBeforeFieldInit)
    }
    pub fn IsTdForwarder(x: CorTypeAttr) -> bool {
        x.contains(CorTypeAttr::tdForwarder)
    }
    pub fn IsTdRTSpecialName(x: CorTypeAttr) -> bool {
        x.contains(CorTypeAttr::tdRTSpecialName)
    }
    pub fn IsTdHasSecurity(x: CorTypeAttr) -> bool {
        x.contains(CorTypeAttr::tdHasSecurity)
    }
}

bitflags! {
    /// Indicates the type of a metadata token.
    pub struct CorTokenType: DWORD {
        const mdtModule                       = 0x00000000;
        const mdtTypeRef                      = 0x01000000;
        const mdtTypeDef                      = 0x02000000;
        const mdtFieldDef                     = 0x04000000;
        const mdtMethodDef                    = 0x06000000;
        const mdtParamDef                     = 0x08000000;
        const mdtInterfaceImpl                = 0x09000000;
        const mdtMemberRef                    = 0x0a000000;
        const mdtCustomAttribute              = 0x0c000000;
        const mdtPermission                   = 0x0e000000;
        const mdtSignature                    = 0x11000000;
        const mdtEvent                        = 0x14000000;
        const mdtProperty                     = 0x17000000;
        const mdtModuleRef                    = 0x1a000000;
        const mdtTypeSpec                     = 0x1b000000;
        const mdtAssembly                     = 0x20000000;
        const mdtAssemblyRef                  = 0x23000000;
        const mdtFile                         = 0x26000000;
        const mdtExportedType                 = 0x27000000;
        const mdtManifestResource             = 0x28000000;
        const mdtGenericParam                 = 0x2a000000;
        const mdtMethodSpec                   = 0x2b000000;
        const mdtGenericParamConstraint       = 0x2c000000;
        const mdtString                       = 0x70000000;
        const mdtName                         = 0x71000000;
        const mdtBaseType                     = 0x72000000;
    }
}

bitflags! {
    pub struct CorCallingConvention : COR_SIGNATURE {
        /// Indicates a default calling convention.
        const IMAGE_CEE_CS_CALLCONV_DEFAULT = 0x0;
        /// Indicates that the method takes a variable number of parameters.
        const IMAGE_CEE_CS_CALLCONV_VARARG = 0x5;
        /// Indicates that the call is to a field.
        const IMAGE_CEE_CS_CALLCONV_FIELD = 0x6;
        /// Indicates that the call is to a local method.
        const IMAGE_CEE_CS_CALLCONV_LOCAL_SIG = 0x7;
        /// Indicates that the call is to a property.
        const IMAGE_CEE_CS_CALLCONV_PROPERTY = 0x8;
        /// Indicates that the call is unmanaged.
        const IMAGE_CEE_CS_CALLCONV_UNMANAGED = 0x9;
        /// Indicates a generic method instantiation.
        const IMAGE_CEE_CS_CALLCONV_GENERICINST = 0xa;
        /// Indicates a 64-bit PInvoke call to a method that takes a variable number of parameters.
        const IMAGE_CEE_CS_CALLCONV_NATIVEVARARG = 0xb;
        /// Describes an invalid 4-bit value.
        const IMAGE_CEE_CS_CALLCONV_MAX = 0xc;
        /// Indicates that the calling convention is described by the bottom four bits.
        const IMAGE_CEE_CS_CALLCONV_MASK = 0x0f;
        /// Indicates that the top bit describes a 'this' parameter.
        const IMAGE_CEE_CS_CALLCONV_HASTHIS = 0x20;
        /// Indicates that a 'this' parameter is explicitly described in the signature.
        const IMAGE_CEE_CS_CALLCONV_EXPLICITTHIS = 0x40;
        /// Indicates a generic method signature with an explicit number of type arguments.
        /// This precedes an ordinary parameter count.
        const IMAGE_CEE_CS_CALLCONV_GENERIC = 0x10;
    }
}

impl CorCallingConvention {
    /// Whether the calling convention is generic
    pub fn is_generic(&self) -> bool {
        self.contains(CorCallingConvention::IMAGE_CEE_CS_CALLCONV_GENERIC)
    }
}

/// Gets the type from the token
/// https://github.com/dotnet/coreclr/blob/a9f3fc16483eecfc47fb79c362811d870be02249/src/inc/corhdr.h#L1516
pub fn type_from_token(token: mdToken) -> ULONG32 {
    token & 0xff000000
}

pub fn rid_from_token(token: mdToken) -> RID {
    token & 0x00ffffff
}

pub fn token_from_rid(rid: RID, token_type: mdToken) -> mdToken {
    rid | token_type
}

pub fn rid_to_token(rid: RID, token_type: mdToken) -> mdToken {
    rid | token_type
}

pub fn is_nil_token(token: mdToken) -> bool {
    rid_from_token(token) == 0
}

// allow types defined here to be passed across FFI boundary
macro_rules! primitive_transferable_type {
    ($($t:ty),+) => {
        $(unsafe impl AbiTransferable for $t {
            type Abi = Self;
            fn get_abi(&self) -> Self::Abi {
                *self
            }
            fn set_abi(&mut self) -> *mut Self::Abi {
                self as *mut Self::Abi
            }
        })*
    };
}

primitive_transferable_type! {
    COR_PRF_JIT_CACHE,
    COR_PRF_TRANSITION_REASON,
    COR_PRF_SUSPEND_REASON,
    COR_PRF_GC_REASON,
    CorSaveSize
}

#[cfg(test)]
mod tests {
    use crate::ffi::CorTypeAttr;

    #[test]
    fn CorTypeAttr_flags() {
        let flags = CorTypeAttr::tdSealed | CorTypeAttr::tdPublic;
        assert!(CorTypeAttr::IsTdSealed(flags))
    }
}
