// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

use std::{ffi::c_void, mem::MaybeUninit, ptr};

use com::{
    interfaces,
    interfaces::IUnknown,
    sys::{FAILED, HRESULT, S_OK},
    Interface,
};
use widestring::U16CString;

use crate::{
    cil::MAX_LENGTH,
    ffi::*,
    interfaces::{
        ICorProfilerFunctionEnum, ICorProfilerMethodEnum, ICorProfilerModuleEnum,
        ICorProfilerObjectEnum, ICorProfilerThreadEnum, IMetaDataImport, IMethodMalloc,
    },
    types::{
        AppDomainInfo, ArrayClassInfo, AssemblyInfo, ClassInfo, FunctionInfo,
        FunctionTokenAndMetadata, IlFunctionBody, ModuleInfo, ModuleInfo2, RuntimeInfo,
    },
};

interfaces! {
    #[uuid("28B5557D-3F3F-48b4-90B2-5F9EEA2F6C48")]
    pub unsafe interface ICorProfilerInfo: IUnknown {
        pub fn GetClassFromObject(&self, objectId: ObjectID, pClassId: *mut ClassID) -> HRESULT;
        pub fn GetClassFromToken(&self,
            moduleId: ModuleID,
            typeDef: mdTypeDef,
            pClassId: *mut ClassID,
        ) -> HRESULT;
        pub fn GetCodeInfo(&self,
            functionId: FunctionID,
            pStart: *mut LPCBYTE,
            pcSize: *mut ULONG,
        ) -> HRESULT;
        pub fn GetEventMask(&self, pdwEvents: *mut DWORD) -> HRESULT;
        pub fn GetFunctionFromIP(&self, ip: LPCBYTE, pFunctionId: *mut FunctionID) -> HRESULT;
        pub fn GetFunctionFromToken(&self,
            moduleId: ModuleID,
            token: mdToken,
            pFunctionId: *mut FunctionID,
        ) -> HRESULT;
        pub fn GetHandleFromThread(&self, threadId: ThreadID, phThread: *mut HANDLE) -> HRESULT;
        pub fn GetObjectSize(&self, objectId: ObjectID, pcSize: *mut ULONG) -> HRESULT;
        pub fn IsArrayClass(&self,
            classId: ClassID,
            pBaseElemType: *mut CorElementType,
            pBaseClassId: *mut ClassID,
            pcRank: *mut ULONG,
        ) -> HRESULT;
        pub fn GetThreadInfo(&self,
            threadId: ThreadID,
            pdwWin32ThreadId: *mut DWORD,
        ) -> HRESULT;
        pub fn GetCurrentThreadID(&self, pThreadId: *mut ThreadID) -> HRESULT;
        pub fn GetClassIDInfo(&self,
            classId: ClassID,
            pModuleId: *mut ModuleID,
            pTypeDefToken: *mut mdTypeDef,
        ) -> HRESULT;
        pub fn GetFunctionInfo(&self,
            functionId: FunctionID,
            pClassId: *mut ClassID,
            pModuleId: *mut ModuleID,
            pToken: *mut mdToken,
        ) -> HRESULT;
        pub fn SetEventMask(&self, dwEvents: DWORD) -> HRESULT;
        pub fn SetEnterLeaveFunctionHooks(&self,
            pFuncEnter: *const FunctionEnter,
            pFuncLeave: *const FunctionLeave,
            pFuncTailcall: *const FunctionTailcall,
        ) -> HRESULT;
        pub fn SetFunctionIDMapper(&self, pFunc: *const FunctionIDMapper) -> HRESULT;
        pub fn GetTokenAndMetaDataFromFunction(&self,
            functionId: FunctionID,
            riid: REFIID,
            ppImport: *mut *mut IUnknown,
            pToken: *mut mdToken,
        ) -> HRESULT;
        pub fn GetModuleInfo(&self,
            moduleId: ModuleID,
            ppBaseLoadAddress: *mut LPCBYTE,
            cchName: ULONG,
            pcchName: *mut ULONG,
            szName: *mut WCHAR,
            pAssemblyId: *mut AssemblyID,
        ) -> HRESULT;
        pub fn GetModuleMetaData(&self,
            moduleId: ModuleID,
            dwOpenFlags: DWORD,
            riid: REFIID,
            ppOut: *mut *mut IUnknown,
        ) -> HRESULT;
        /// Gets a pointer to the body of a method in Microsoft intermediate language (MSIL) code,
        /// starting at its header.
        pub fn GetILFunctionBody(&self,
            moduleId: ModuleID,
            methodId: mdMethodDef,
            ppMethodHeader: *mut LPCBYTE,
            pcbMethodSize: *mut ULONG,
        ) -> HRESULT;
        pub fn GetILFunctionBodyAllocator(&self,
            moduleId: ModuleID,
            ppMalloc: *mut *mut IMethodMalloc,
        ) -> HRESULT;
        pub fn SetILFunctionBody(&self,
            moduleId: ModuleID,
            methodid: mdMethodDef,
            pbNewILMethodHeader: LPCBYTE,
        ) -> HRESULT;
        pub fn GetAppDomainInfo(&self,
            appDomainId: AppDomainID,
            cchName: ULONG,
            pcchName: *mut ULONG,
            szName: *mut WCHAR,
            pProcessId: *mut ProcessID,
        ) -> HRESULT;
        pub fn GetAssemblyInfo(&self,
            assemblyId: AssemblyID,
            cchName: ULONG,
            pcchName: *mut ULONG,
            szName: *mut WCHAR,
            pAppDomainId: *mut AppDomainID,
            pModuleId: *mut ModuleID,
        ) -> HRESULT;
        pub fn SetFunctionReJIT(&self, functionId: FunctionID) -> HRESULT;
        pub fn ForceGC(&self) -> HRESULT;
        pub fn SetILInstrumentedCodeMap(&self,
            functionId: FunctionID,
            fStartJit: BOOL,
            cILMapEntries: ULONG,
            rgILMapEntries: *const COR_IL_MAP,
        ) -> HRESULT;
        pub fn GetInprocInspectionInterface(&self,
            ppicd: *mut *mut IUnknown,
        ) -> HRESULT;
        pub fn GetInprocInspectionIThisThread(&self,
            ppicd: *mut *mut IUnknown,
        ) -> HRESULT;
        pub fn GetThreadContext(&self,
            threadId: ThreadID,
            pContextId: *mut ContextID,
        ) -> HRESULT;
        pub fn BeginInprocDebugging(&self,
            fThisThreadOnly: BOOL,
            pdwProfilerContext: *mut DWORD,
        ) -> HRESULT;
        pub fn EndInprocDebugging(&self, dwProfilerContext: DWORD) -> HRESULT;
        pub fn GetILToNativeMapping(&self,
            functionId: FunctionID,
            cMap: ULONG32,
            pcMap: *mut ULONG32,
            map: *mut COR_DEBUG_IL_TO_NATIVE_MAP,
        ) -> HRESULT;
    }

    #[uuid("CC0935CD-A518-487d-B0BB-A93214E65478")]
    pub unsafe interface ICorProfilerInfo2: ICorProfilerInfo {
        pub fn DoStackSnapshot(&self,
        thread: ThreadID,
        callback: *const StackSnapshotCallback,
        infoFlags: ULONG32,
        clientData: *const c_void,
        context: *const BYTE,
        contextSize: ULONG32,
        ) -> HRESULT;
        pub fn SetEnterLeaveFunctionHooks2(&self,
            pFuncEnter: *const FunctionEnter2,
            pFuncLeave: *const FunctionLeave2,
            pFuncTailcall: *const FunctionTailcall2,
        ) -> HRESULT;
        pub fn GetFunctionInfo2(&self,
            funcId: FunctionID,
            frameInfo: COR_PRF_FRAME_INFO,
            pClassId: *mut ClassID,
            pModuleId: *mut ModuleID,
            pToken: *mut mdToken,
            cTypeArgs: ULONG32,
            pcTypeArgs: *mut ULONG32,
            typeArgs: *mut ClassID,
        ) -> HRESULT;
        pub fn GetStringLayout(&self,
            pBufferLengthOffset: *mut ULONG,
            pStringLengthOffset: *mut ULONG,
            pBufferOffset: *mut ULONG,
        ) -> HRESULT;
        pub fn GetClassLayout(&self,
            classID: ClassID,
            rFieldOffset: *mut COR_FIELD_OFFSET,
            cFieldOffset: ULONG,
            pcFieldOffset: *mut ULONG,
            pulClassSize: *mut ULONG,
        ) -> HRESULT;
        pub fn GetClassIDInfo2(&self,
            classId: ClassID,
            pModuleId: *mut ModuleID,
            pTypeDefToken: *mut mdTypeDef,
            pParentClassId: *mut ClassID,
            cNumTypeArgs: ULONG32,
            pcNumTypeArgs: *mut ULONG32,
            typeArgs: *mut ClassID,
        ) -> HRESULT;
        pub fn GetCodeInfo2(&self,
            functionID: FunctionID,
            cCodeInfos: ULONG32,
            pcCodeInfos: *mut ULONG32,
            codeInfos: *mut COR_PRF_CODE_INFO,
        ) -> HRESULT;
        pub fn GetClassFromTokenAndTypeArgs(&self,
            moduleID: ModuleID,
            typeDef: mdTypeDef,
            cTypeArgs: ULONG32,
            typeArgs: *const ClassID,
            pClassID: *mut ClassID,
        ) -> HRESULT;
        pub fn GetFunctionFromTokenAndTypeArgs(&self,
            moduleID: ModuleID,
            funcDef: mdMethodDef,
            classId: ClassID,
            cTypeArgs: ULONG32,
            typeArgs: *const ClassID,
            pFunctionID: *mut FunctionID,
        ) -> HRESULT;
        pub fn EnumModuleFrozenObjects(&self,
            moduleID: ModuleID,
            ppEnum: *mut *mut ICorProfilerObjectEnum,
        ) -> HRESULT;
        pub fn GetArrayObjectInfo(&self,
            objectId: ObjectID,
            cDimensions: ULONG32,
            pDimensionSizes: *mut ULONG32,
            pDimensionLowerBounds: *mut int,
            ppData: *mut *mut BYTE,
        ) -> HRESULT;
        pub fn GetBoxClassLayout(&self,
            classId: ClassID,
            pBufferOffset: *mut ULONG32,
        ) -> HRESULT;
        pub fn GetThreadAppDomain(&self,
            threadId: ThreadID,
            pAppDomainId: *mut AppDomainID,
        ) -> HRESULT;
        pub fn GetRVAStaticAddress(&self,
            classId: ClassID,
            fieldToken: mdFieldDef,
            ppAddress: *mut *mut c_void,
        ) -> HRESULT;
        pub fn GetAppDomainStaticAddress(&self,
            classId: ClassID,
            fieldToken: mdFieldDef,
            appDomainId: AppDomainID,
            ppAddress: *mut *mut c_void,
        ) -> HRESULT;
        pub fn GetThreadStaticAddress(&self,
            classId: ClassID,
            fieldToken: mdFieldDef,
            threadId: ThreadID,
            ppAddress: *mut *mut c_void,
        ) -> HRESULT;
        pub fn GetContextStaticAddress(&self,
            classId: ClassID,
            fieldToken: mdFieldDef,
            contextId: ContextID,
            ppAddress: *mut *mut c_void,
        ) -> HRESULT;
        pub fn GetStaticFieldInfo(&self,
            classId: ClassID,
            fieldToken: mdFieldDef,
            pFieldInfo: *mut COR_PRF_STATIC_TYPE,
        ) -> HRESULT;
        pub fn GetGenerationBounds(&self,
            cObjectRanges: ULONG,
            pcObjectRanges: *mut ULONG,
            ranges: *mut COR_PRF_GC_GENERATION_RANGE,
        ) -> HRESULT;
        pub fn GetObjectGeneration(&self,
            objectId: ObjectID,
            range: *mut COR_PRF_GC_GENERATION_RANGE,
        ) -> HRESULT;
        pub fn GetNotifiedExceptionClauseInfo(&self, pinfo: *mut COR_PRF_EX_CLAUSE_INFO) -> HRESULT;
    }

    #[uuid("B555ED4F-452A-4E54-8B39-B5360BAD32A0")]
    pub unsafe interface ICorProfilerInfo3: ICorProfilerInfo2 {
        pub fn EnumJITedFunctions(&self, ppEnum: *mut *mut ICorProfilerFunctionEnum) -> HRESULT;
        pub fn RequestProfilerDetach(&self, dwExpectedCompletionMilliseconds: DWORD) -> HRESULT;
        pub fn SetFunctionIDMapper2(&self,
            pFunc: *const FunctionIDMapper2,
            clientData: *const c_void,
        ) -> HRESULT;
        pub fn GetStringLayout2(&self,
            pStringLengthOffset: *mut ULONG,
            pBufferOffset: *mut ULONG,
        ) -> HRESULT;
        pub fn SetEnterLeaveFunctionHooks3(&self,
            pFuncEnter3: *const FunctionEnter3,
            pFuncLeave3: *const FunctionLeave3,
            pFuncTailcall3: *const FunctionTailcall3,
        ) -> HRESULT;
        pub fn SetEnterLeaveFunctionHooks3WithInfo(&self,
            pFuncEnter3WithInfo: *const FunctionEnter3WithInfo,
            pFuncLeave3WithInfo: *const FunctionLeave3WithInfo,
            pFuncTailcall3WithInfo: *const FunctionTailcall3WithInfo,
        ) -> HRESULT;
        pub fn GetFunctionEnter3Info(&self,
            functionId: FunctionID,
            eltInfo: COR_PRF_ELT_INFO,
            pFrameInfo: *mut COR_PRF_FRAME_INFO,
            pcbArgumentInfo: *mut ULONG,
            pArgumentInfo: *mut COR_PRF_FUNCTION_ARGUMENT_INFO,
        ) -> HRESULT;
        pub fn GetFunctionLeave3Info(&self,
            functionId: FunctionID,
            eltInfo: COR_PRF_ELT_INFO,
            pFrameInfo: *mut COR_PRF_FRAME_INFO,
            pRetvalRange: *mut COR_PRF_FUNCTION_ARGUMENT_RANGE,
        ) -> HRESULT;
        pub fn GetFunctionTailcall3Info(&self,
            functionId: FunctionID,
            eltInfo: COR_PRF_ELT_INFO,
            pFrameInfo: *mut COR_PRF_FRAME_INFO,
        ) -> HRESULT;
        pub fn EnumModules(&self, ppEnum: *mut *mut ICorProfilerModuleEnum) -> HRESULT;
        pub fn GetRuntimeInformation(&self,
            pClrInstanceId: *mut USHORT,
            pRuntimeType: *mut COR_PRF_RUNTIME_TYPE,
            pMajorVersion: *mut USHORT,
            pMinorVersion: *mut USHORT,
            pBuildNumber: *mut USHORT,
            pQFEVersion: *mut USHORT,
            cchVersionString: ULONG,
            pcchVersionString: *mut ULONG,
            szVersionString: *mut WCHAR,
        ) -> HRESULT;
        pub fn GetThreadStaticAddress2(&self,
            classId: ClassID,
            fieldToken: mdFieldDef,
            appDomainId: AppDomainID,
            threadId: ThreadID,
            ppAddress: *mut *mut c_void,
        ) -> HRESULT;
        pub fn GetAppDomainsContainingModule(&self,
            moduleId: ModuleID,
            cAppDomainIds: ULONG32,
            pcAppDomainIds: *mut ULONG32,
            appDomainIds: *mut AppDomainID,
        ) -> HRESULT;
        pub fn GetModuleInfo2(&self,
            moduleId: ModuleID,
            ppBaseLoadAddress: *mut LPCBYTE,
            cchName: ULONG,
            pcchName: *mut ULONG,
            szName: *mut WCHAR,
            pAssemblyId: *mut AssemblyID,
            pdwModuleFlags: *mut DWORD,
        ) -> HRESULT;
    }

    #[uuid("0D8FDCAA-6257-47BF-B1BF-94DAC88466EE")]
    pub unsafe interface ICorProfilerInfo4: ICorProfilerInfo3 {
        pub fn EnumThreads(&self, ppEnum: *mut *mut ICorProfilerThreadEnum) -> HRESULT;

        pub fn InitializeCurrentThread(&self) -> HRESULT;

        pub fn RequestReJIT(&self,
            cFunctions: ULONG,
            moduleIds: *const ModuleID,
            methodIds: *const mdMethodDef,
        ) -> HRESULT;

        pub fn RequestRevert(&self,
            cFunctions: ULONG,
            moduleIds: *const ModuleID,
            methodIds: *const mdMethodDef,
            status: *mut HRESULT,
        ) -> HRESULT;

        pub fn GetCodeInfo3(&self,
            functionID: FunctionID,
            reJitId: ReJITID,
            cCodeInfos: ULONG32,
            pcCodeInfos: *mut ULONG32,
            codeInfos: *mut COR_PRF_CODE_INFO,
        ) -> HRESULT;

        pub fn GetFunctionFromIP2(&self,
            ip: LPCBYTE,
            pFunctionId: *mut FunctionID,
            pReJitId: *mut ReJITID,
        ) -> HRESULT;

        pub fn GetReJITIDs(&self,
            functionId: FunctionID,
            cReJitIds: ULONG,
            pcReJitIds: *mut ULONG,
            reJitIds: *mut ReJITID,
        ) -> HRESULT;

        pub fn GetILToNativeMapping2(&self,
            functionId: FunctionID,
            reJitId: ReJITID,
            cMap: ULONG32,
            pcMap: *mut ULONG32,
            map: *mut COR_DEBUG_IL_TO_NATIVE_MAP,
        ) -> HRESULT;

        pub fn EnumJITedFunctions2(&self, ppEnum: *mut *mut ICorProfilerFunctionEnum) -> HRESULT;

        pub fn GetObjectSize2(&self, objectId: ObjectID, pcSize: *mut SIZE_T) -> HRESULT;
    }

    #[uuid("07602928-CE38-4B83-81E7-74ADAF781214")]
    pub unsafe interface ICorProfilerInfo5: ICorProfilerInfo4 {
        pub fn GetEventMask2(&self, pdwEventsLow: *mut DWORD, pdwEventsHigh: *mut DWORD) -> HRESULT;
        pub fn SetEventMask2(&self, dwEventsLow: DWORD, dwEventsHigh: DWORD) -> HRESULT;
    }

    #[uuid("F30A070D-BFFB-46A7-B1D8-8781EF7B698A")]
    pub unsafe interface ICorProfilerInfo6: ICorProfilerInfo5 {
        pub fn EnumNgenModuleMethodsInliningThisMethod(&self,
            inlinersModuleId: ModuleID,
            inlineeModuleId: ModuleID,
            inlineeMethodId: mdMethodDef,
            incompleteData: *mut BOOL,
            ppEnum: *mut *mut ICorProfilerMethodEnum) -> HRESULT;
    }

    #[uuid("9AEECC0D-63E0-4187-8C00-E312F503F663")]
    pub unsafe interface ICorProfilerInfo7: ICorProfilerInfo6 {
        pub fn ApplyMetaData(&self, moduleId: ModuleID) -> HRESULT;
        pub fn GetInMemorySymbolsLength(&self,
            moduleId: ModuleID,
            pCountSymbolBytes: *mut DWORD) -> HRESULT;
        pub fn ReadInMemorySymbols(&self,
            moduleId: ModuleID,
            symbolsReadOffset: DWORD,
            pSymbolBytes: *mut BYTE,
            countSymbolBytes: DWORD,
            pCountSymbolBytesRead: *mut DWORD) -> HRESULT;
    }

    #[uuid("C5AC80A6-782E-4716-8044-39598C60CFBF")]
    pub unsafe interface ICorProfilerInfo8: ICorProfilerInfo7 {
        /// Determines if a function has associated metadata.
        ///
        /// Certain methods like IL Stubs or LCG Methods do not have
        /// associated metadata that can be retrieved using the IMetaDataImport APIs.
        ///
        /// Such methods can be encountered by profilers through instruction pointers
        /// or by listening to ICorProfilerCallback::DynamicMethodJITCompilationStarted.
        ///
        /// This API can be used to determine whether a FunctionID is dynamic.
        pub fn IsFunctionDynamic(&self,
            functionId: FunctionID,
            isDynamic: *mut BOOL) -> HRESULT;

        /// Maps a managed code instruction pointer to a FunctionID.
        ///
        /// GetFunctionFromIP2 fails for dynamic methods, this method works for
        /// both dynamic and non-dynamic methods. It is a superset of GetFunctionFromIP2
        pub fn GetFunctionFromIP3(&self,
            ip: LPCBYTE,
            functionId: *mut FunctionID,
            pReJitId: *mut ReJITID) -> HRESULT;

        /// Retrieves information about dynamic methods
        ///
        /// Certain methods like IL Stubs or LCG do not have
        /// associated metadata that can be retrieved using the IMetaDataImport APIs.
        ///
        /// Such methods can be encountered by profilers through instruction pointers
        /// or by listening to ICorProfilerCallback::DynamicMethodJITCompilationStarted
        ///
        /// This API can be used to retrieve information about dynamic methods
        /// including a friendly name if available.
        ///
         pub fn GetDynamicFunctionInfo(&self,
            functionId: FunctionID,
            moduleId: *mut ModuleID,
            ppvSig: *mut PCCOR_SIGNATURE,
            pbSig: *mut ULONG,
            cchName: ULONG,
            pcchName: *mut ULONG,
            wszName: *mut WCHAR) -> HRESULT;
    }

    #[uuid("008170DB-F8CC-4796-9A51-DC8AA0B47012")]
    pub unsafe interface ICorProfilerInfo9: ICorProfilerInfo8 {
        /// Given functionId + rejitId, enumerate the native code start address of all
        /// jitted versions of this code that currently exist
        pub fn GetNativeCodeStartAddresses(&self,
                functionID: FunctionID,
                reJitId: ReJITID,
                cCodeStartAddresses: ULONG32,
                pcCodeStartAddresses: *mut ULONG32,
                codeStartAddresses: *mut UINT_PTR) -> HRESULT;

        /// Given the native code start address,
        /// return the native->IL mapping information for this jitted version of the code
        pub fn GetILToNativeMapping3(&self,
            pNativeCodeStartAddress: UINT_PTR,
            cMap: ULONG32,
            pcMap: *mut ULONG32,
            map: *mut COR_DEBUG_IL_TO_NATIVE_MAP) -> HRESULT;

        /// Given the native code start address, return the the blocks of virtual memory
        /// that store this code (method code is not necessarily stored in a single contiguous memory region)
        pub fn GetCodeInfo4(&self,
            pNativeCodeStartAddress: UINT_PTR,
            cCodeInfos: ULONG32,
            pcCodeInfos: *mut ULONG32,
            codeInfos: *mut COR_PRF_CODE_INFO) -> HRESULT;
    }

    #[uuid("2F1B5152-C869-40C9-AA5F-3ABE026BD720")]
    pub unsafe interface ICorProfilerInfo10: ICorProfilerInfo9 {
        /// Given an ObjectID, callback and clientData, enumerates each object reference (if any).
        pub fn EnumerateObjectReferences(&self,
            objectId: ObjectID,
            // TODO: double
            callback: *const ObjectReferenceCallback,
            clientData: *const c_void) -> HRESULT;

        /// Given an ObjectID, determines whether it is in a read only segment.
        pub fn IsFrozenObject(&self, objectId: ObjectID, pbFrozen: *mut BOOL) -> HRESULT;

        /// Gets the value of the configured LOH Threshold.
        pub fn GetLOHObjectSizeThreshold(&self, pThreshold: *mut DWORD) -> HRESULT;

        /// This method will ReJIT the methods requested, as well as any inliners
        /// of the methods requested.
        ///
        /// RequestReJIT does not do any tracking of inlined methods. The profiler
        /// was expected to track inlining and call RequestReJIT for all inliners
        /// to make sure every instance of an inlined method was ReJITted.
        /// This poses a problem with ReJIT on attach, since the profiler was
        /// not present to monitor inlining. This method can be called to guarantee
        /// that the full set of inliners will be ReJITted as well.
        ///
        pub fn RequestReJITWithInliners(&self,
            dwRejitFlags: DWORD,
            cFunctions: ULONG,
            moduleIds: *const ModuleID,
            methodIds: *const mdMethodDef) -> HRESULT;

        /// Suspend the runtime without performing a GC.
        pub fn SuspendRuntime(&self) -> HRESULT;

        /// Restart the runtime from a previous suspension.
        pub fn ResumeRuntime(&self) -> HRESULT;
    }

    #[uuid("06398876-8987-4154-B621-40A00D6E4D04")]
    pub unsafe interface ICorProfilerInfo11: ICorProfilerInfo10 {
        /// Get environment variable for the running managed code.
        pub fn GetEnvironmentVariable(&self,
            szName: *const WCHAR,
            cchValue: ULONG,
            pcchValue: *mut ULONG,
            szValue: *mut WCHAR) -> HRESULT;

        /// Set environment variable for the running managed code.
        ///
        /// The code profiler calls this function to modify environment variables of the
        /// current managed process. For example, it can be used in the profiler's Initialize()
        /// or InitializeForAttach() callbacks.
        ///
        /// szName is the name of the environment variable, should not be NULL.
        ///
        /// szValue is the contents of the environment variable, or NULL if the variable should be deleted.
        pub fn SetEnvironmentVariable(&self,
            szName: *const WCHAR,
            szValue: *const WCHAR) -> HRESULT;
    }
}

/// Rust abstractions over COM functions
impl ICorProfilerInfo {
    fn get_class_from_object(&self, object_id: ObjectID) -> Result<ClassID, HRESULT> {
        let mut class_id = MaybeUninit::uninit();
        let hr = unsafe { self.GetClassFromObject(object_id, class_id.as_mut_ptr()) };
        match hr {
            S_OK => {
                let class_id = unsafe { class_id.assume_init() };
                Ok(class_id)
            }
            _ => Err(hr),
        }
    }
    pub fn get_event_mask(&self) -> Result<COR_PRF_MONITOR, HRESULT> {
        let mut events = MaybeUninit::uninit();
        let hr = unsafe { self.GetEventMask(events.as_mut_ptr()) };
        match hr {
            S_OK => {
                let events = unsafe { events.assume_init() };
                Ok(COR_PRF_MONITOR::from_bits(events).unwrap())
            }
            _ => Err(hr),
        }
    }
    pub fn get_function_from_ip(&self, ip: LPCBYTE) -> Result<FunctionID, HRESULT> {
        let mut function_id = MaybeUninit::uninit();
        let hr = unsafe { self.GetFunctionFromIP(ip, function_id.as_mut_ptr()) };
        match hr {
            S_OK => {
                let function_id = unsafe { function_id.assume_init() };
                Ok(function_id)
            }
            _ => Err(hr),
        }
    }
    pub fn get_handle_from_thread(&self, thread_id: ThreadID) -> Result<HANDLE, HRESULT> {
        let mut handle = MaybeUninit::uninit();
        let hr = unsafe { self.GetHandleFromThread(thread_id, handle.as_mut_ptr()) };
        match hr {
            S_OK => {
                let handle = unsafe { handle.assume_init() };
                Ok(handle)
            }
            _ => Err(hr),
        }
    }
    pub fn is_array_class(&self, class_id: ClassID) -> Result<ArrayClassInfo, HRESULT> {
        let mut element_type = MaybeUninit::uninit();
        let mut element_class_id = MaybeUninit::uninit();
        let mut rank = MaybeUninit::uninit();
        let hr = unsafe {
            self.IsArrayClass(
                class_id,
                element_type.as_mut_ptr(),
                element_class_id.as_mut_ptr(),
                rank.as_mut_ptr(),
            )
        };
        match hr {
            S_OK => {
                let element_type = unsafe { element_type.assume_init() };
                let element_class_id = unsafe {
                    if !element_class_id.as_ptr().is_null() {
                        Some(element_class_id.assume_init())
                    } else {
                        None
                    }
                };
                let rank = unsafe { rank.assume_init() };
                Ok(ArrayClassInfo {
                    element_type,
                    element_class_id,
                    rank,
                })
            }
            _ => Err(hr),
        }
    }
    pub fn get_thread_info(&self, thread_id: ThreadID) -> Result<DWORD, HRESULT> {
        let mut win_32_thread_id = MaybeUninit::uninit();
        let hr = unsafe { self.GetThreadInfo(thread_id, win_32_thread_id.as_mut_ptr()) };
        match hr {
            S_OK => {
                let win_32_thread_id = unsafe { win_32_thread_id.assume_init() };
                Ok(win_32_thread_id)
            }
            _ => Err(hr),
        }
    }
    pub fn get_current_thread_id(&self) -> Result<ThreadID, HRESULT> {
        let mut thread_id = MaybeUninit::uninit();
        let hr = unsafe { self.GetCurrentThreadID(thread_id.as_mut_ptr()) };
        match hr {
            S_OK => {
                let thread_id = unsafe { thread_id.assume_init() };
                Ok(thread_id)
            }
            _ => Err(hr),
        }
    }
    pub fn get_class_id_info(&self, class_id: ClassID) -> Result<ClassInfo, HRESULT> {
        let mut module_id = MaybeUninit::uninit();
        let mut token = MaybeUninit::uninit();
        let hr =
            unsafe { self.GetClassIDInfo(class_id, module_id.as_mut_ptr(), token.as_mut_ptr()) };
        match hr {
            S_OK => {
                let module_id = unsafe { module_id.assume_init() };
                let token = unsafe { token.assume_init() };
                Ok(ClassInfo { module_id, token })
            }
            _ => Err(hr),
        }
    }
    pub fn get_function_info(&self, function_id: FunctionID) -> Result<FunctionInfo, HRESULT> {
        let mut class_id = MaybeUninit::uninit();
        let mut module_id = MaybeUninit::uninit();
        let mut token = MaybeUninit::uninit();
        let hr = unsafe {
            self.GetFunctionInfo(
                function_id,
                class_id.as_mut_ptr(),
                module_id.as_mut_ptr(),
                token.as_mut_ptr(),
            )
        };
        match hr {
            S_OK => {
                let class_id = unsafe { class_id.assume_init() };
                let module_id = unsafe { module_id.assume_init() };
                let token = unsafe { token.assume_init() };
                Ok(FunctionInfo {
                    class_id,
                    module_id,
                    token,
                })
            }
            _ => Err(hr),
        }
    }
    pub fn set_event_mask(&self, events: COR_PRF_MONITOR) -> Result<(), HRESULT> {
        let events = events.bits();
        let hr = unsafe { self.SetEventMask(events) };
        match hr {
            S_OK => Ok(()),
            _ => Err(hr),
        }
    }
    pub fn set_enter_leave_function_hooks(
        &self,
        func_enter: FunctionEnter,
        func_leave: FunctionLeave,
        func_tailcall: FunctionTailcall,
    ) -> Result<(), HRESULT> {
        let func_enter = func_enter as *const FunctionEnter;
        let func_leave = func_leave as *const FunctionLeave;
        let func_tailcall = func_tailcall as *const FunctionTailcall;
        let hr = unsafe { self.SetEnterLeaveFunctionHooks(func_enter, func_leave, func_tailcall) };
        match hr {
            S_OK => Ok(()),
            _ => Err(hr),
        }
    }
    pub fn set_function_id_mapper(&self, func: FunctionIDMapper) -> Result<(), HRESULT> {
        let func = func as *const FunctionIDMapper;
        let hr = unsafe { self.SetFunctionIDMapper(func) };
        match hr {
            S_OK => Ok(()),
            _ => Err(hr),
        }
    }
    pub fn get_token_and_metadata_from_function(
        &self,
        function_id: FunctionID,
    ) -> Result<FunctionTokenAndMetadata, HRESULT> {
        let mut metadata_import = MaybeUninit::uninit();
        let mut token = MaybeUninit::uninit();
        let riid = IMetaDataImport::IID; // TODO: This needs to come from an IMetaDataImport implementation
        let hr = unsafe {
            self.GetTokenAndMetaDataFromFunction(
                function_id,
                &riid,
                metadata_import.as_mut_ptr(),
                token.as_mut_ptr(),
            )
        };

        match hr {
            S_OK => {
                let metadata_import = unsafe { metadata_import.assume_init() };
                let token = unsafe { token.assume_init() };
                Ok(FunctionTokenAndMetadata {
                    metadata_import,
                    token,
                })
            }
            _ => Err(hr),
        }
    }
    pub fn get_module_info(&self, module_id: ModuleID) -> Result<ModuleInfo, HRESULT> {
        let mut name_buffer_length = MaybeUninit::uninit();
        unsafe {
            self.GetModuleInfo(
                module_id,
                ptr::null_mut(),
                0,
                name_buffer_length.as_mut_ptr(),
                ptr::null_mut(),
                ptr::null_mut(),
            )
        };

        let mut base_load_address = MaybeUninit::uninit();
        let name_buffer_length = unsafe { name_buffer_length.assume_init() };
        let mut name_buffer = Vec::<WCHAR>::with_capacity(name_buffer_length as usize);
        unsafe { name_buffer.set_len(name_buffer_length as usize) };
        let mut name_length = MaybeUninit::uninit();
        let mut assembly_id = MaybeUninit::uninit();
        let hr = unsafe {
            self.GetModuleInfo(
                module_id,
                base_load_address.as_mut_ptr(),
                name_buffer_length,
                name_length.as_mut_ptr(),
                name_buffer.as_mut_ptr(),
                assembly_id.as_mut_ptr(),
            )
        };
        match hr {
            S_OK => {
                let base_load_address = unsafe { base_load_address.assume_init() };
                let file_name = U16CString::from_vec_with_nul(name_buffer)
                    .unwrap()
                    .to_string_lossy();
                let assembly_id = unsafe { assembly_id.assume_init() };
                Ok(ModuleInfo {
                    base_load_address,
                    file_name,
                    assembly_id,
                })
            }
            _ => Err(hr),
        }
    }
    pub fn get_module_metadata<I: Interface>(
        &self,
        module_id: ModuleID,
        open_flags: CorOpenFlags,
    ) -> Result<I, HRESULT> {
        let mut unknown = None;
        let hr = unsafe {
            self.GetModuleMetaData(
                module_id,
                open_flags.bits(),
                &I::IID as REFIID,
                &mut unknown as *mut _ as *mut *mut IUnknown,
            )
        };

        if FAILED(hr) {
            log::error!(
                "error fetching metadata for module_id {}, HRESULT: {:X}",
                module_id,
                hr
            );
            return Err(hr);
        }
        Ok(unknown.unwrap())
    }

    /// Gets a pointer to the body of a method in Microsoft intermediate language (MSIL) code,
    /// starting at its header.
    pub fn get_il_function_body(
        &self,
        module_id: ModuleID,
        method_id: mdMethodDef,
    ) -> Result<IlFunctionBody, HRESULT> {
        let mut method_header = MaybeUninit::uninit();
        let mut method_size = 0;
        let hr = unsafe {
            self.GetILFunctionBody(
                module_id,
                method_id,
                method_header.as_mut_ptr(),
                &mut method_size,
            )
        };

        match hr {
            S_OK => {
                let method_header = unsafe { method_header.assume_init() };
                Ok(IlFunctionBody {
                    method_header,
                    method_size,
                })
            }
            _ => Err(hr),
        }
    }
    pub fn get_il_function_body_allocator(
        &self,
        module_id: ModuleID,
    ) -> Result<IMethodMalloc, HRESULT> {
        let mut malloc = None;
        let hr = unsafe {
            self.GetILFunctionBodyAllocator(
                module_id,
                &mut malloc as *mut _ as *mut *mut IMethodMalloc,
            )
        };
        match hr {
            S_OK => Ok(malloc.unwrap()),
            _ => Err(hr),
        }
    }
    pub fn set_il_function_body(
        &self,
        module_id: ModuleID,
        method_id: mdMethodDef,
        new_il_method_header: LPCBYTE,
    ) -> Result<(), HRESULT> {
        let hr = unsafe { self.SetILFunctionBody(module_id, method_id, new_il_method_header) };
        match hr {
            S_OK => Ok(()),
            _ => Err(hr),
        }
    }
    pub fn get_app_domain_info(
        &self,
        app_domain_id: AppDomainID,
    ) -> Result<AppDomainInfo, HRESULT> {
        let mut name_buffer_length = MaybeUninit::uninit();
        unsafe {
            self.GetAppDomainInfo(
                app_domain_id,
                0,
                name_buffer_length.as_mut_ptr(),
                ptr::null_mut(),
                ptr::null_mut(),
            )
        };

        let name_buffer_length = unsafe { name_buffer_length.assume_init() };
        let mut name_buffer = Vec::<WCHAR>::with_capacity(name_buffer_length as usize);
        unsafe { name_buffer.set_len(name_buffer_length as usize) };
        let mut name_length = MaybeUninit::uninit();
        let mut process_id = MaybeUninit::uninit();
        let hr = unsafe {
            self.GetAppDomainInfo(
                app_domain_id,
                name_buffer_length,
                name_length.as_mut_ptr(),
                name_buffer.as_mut_ptr(),
                process_id.as_mut_ptr(),
            )
        };
        match hr {
            S_OK => {
                let name = U16CString::from_vec_with_nul(name_buffer)
                    .unwrap()
                    .to_string_lossy();
                let process_id = unsafe { process_id.assume_init() };
                Ok(AppDomainInfo { name, process_id })
            }
            _ => Err(hr),
        }
    }
    pub fn get_assembly_info(&self, assembly_id: AssemblyID) -> Result<AssemblyInfo, HRESULT> {
        let mut name_buffer = Vec::<WCHAR>::with_capacity(MAX_LENGTH as usize);
        let mut name_length = 0;
        let mut app_domain_id = MaybeUninit::uninit();
        let mut module_id = MaybeUninit::uninit();

        let hr = unsafe {
            self.GetAssemblyInfo(
                assembly_id,
                MAX_LENGTH,
                &mut name_length,
                name_buffer.as_mut_ptr(),
                app_domain_id.as_mut_ptr(),
                module_id.as_mut_ptr(),
            )
        };

        match hr {
            S_OK => {
                unsafe { name_buffer.set_len(name_length as usize) };
                let name = U16CString::from_vec_with_nul(name_buffer)
                    .unwrap()
                    .to_string_lossy();
                let app_domain_id = unsafe { app_domain_id.assume_init() };
                let module_id = unsafe { module_id.assume_init() };
                Ok(AssemblyInfo {
                    name,
                    module_id,
                    app_domain_id,
                })
            }
            _ => Err(hr),
        }
    }
    pub fn force_gc(&self) -> Result<(), HRESULT> {
        let hr = unsafe { self.ForceGC() };
        match hr {
            S_OK => Ok(()),
            _ => Err(hr),
        }
    }
    pub fn set_il_instrumented_code_map(
        &self,
        function_id: FunctionID,
        start_jit: bool,
        il_map_entries: &[COR_IL_MAP],
    ) -> Result<(), HRESULT> {
        let start_jit: BOOL = if start_jit { 1 } else { 0 };
        let hr = unsafe {
            self.SetILInstrumentedCodeMap(
                function_id,
                start_jit,
                il_map_entries.len() as ULONG,
                il_map_entries.as_ptr(),
            )
        };
        match hr {
            S_OK => Ok(()),
            _ => Err(hr),
        }
    }
    pub fn get_thread_context(&self, thread_id: ThreadID) -> Result<ContextID, HRESULT> {
        let mut context_id = MaybeUninit::uninit();
        let hr = unsafe { self.GetThreadContext(thread_id, context_id.as_mut_ptr()) };

        match hr {
            S_OK => {
                let context_id = unsafe { context_id.assume_init() };
                Ok(context_id)
            }
            _ => Err(hr),
        }
    }
    pub fn get_il_to_native_mapping(
        &self,
        function_id: FunctionID,
    ) -> Result<Vec<COR_DEBUG_IL_TO_NATIVE_MAP>, HRESULT> {
        let mut map_buffer_length = MaybeUninit::uninit();
        unsafe {
            self.GetILToNativeMapping(
                function_id,
                0,
                map_buffer_length.as_mut_ptr(),
                ptr::null_mut(),
            )
        };

        let map_buffer_length = unsafe { map_buffer_length.assume_init() };
        let mut map = Vec::<COR_DEBUG_IL_TO_NATIVE_MAP>::with_capacity(map_buffer_length as usize);
        unsafe { map.set_len(map_buffer_length as usize) };
        let mut map_length = MaybeUninit::uninit();
        let hr = unsafe {
            self.GetILToNativeMapping(
                function_id,
                map_buffer_length,
                map_length.as_mut_ptr(),
                map.as_mut_ptr(),
            )
        };
        match hr {
            S_OK => Ok(map),
            _ => Err(hr),
        }
    }
}

impl ICorProfilerInfo3 {
    pub fn get_module_info_2(&self, module_id: ModuleID) -> Result<ModuleInfo2, HRESULT> {
        let mut file_name_buffer_length = MaybeUninit::uninit();
        unsafe {
            self.GetModuleInfo2(
                module_id,
                ptr::null_mut(),
                0,
                file_name_buffer_length.as_mut_ptr(),
                ptr::null_mut(),
                ptr::null_mut(),
                ptr::null_mut(),
            )
        };

        let file_name_buffer_length = unsafe { file_name_buffer_length.assume_init() };
        let mut file_name_buffer = Vec::<WCHAR>::with_capacity(file_name_buffer_length as usize);
        unsafe { file_name_buffer.set_len(file_name_buffer_length as usize) };

        let mut base_load_address = MaybeUninit::uninit();
        let mut file_name_length = MaybeUninit::uninit();
        let mut assembly_id = MaybeUninit::uninit();
        let mut module_flags = MaybeUninit::uninit();
        let hr = unsafe {
            self.GetModuleInfo2(
                module_id,
                base_load_address.as_mut_ptr(),
                file_name_buffer_length,
                file_name_length.as_mut_ptr(),
                file_name_buffer.as_mut_ptr(),
                assembly_id.as_mut_ptr(),
                module_flags.as_mut_ptr(),
            )
        };

        match hr {
            S_OK => {
                let base_load_address = unsafe { base_load_address.assume_init() };
                let assembly_id = unsafe { assembly_id.assume_init() };
                let module_flags = unsafe { module_flags.assume_init() };
                let module_flags = COR_PRF_MODULE_FLAGS::from_bits(module_flags).unwrap();
                let file_name = U16CString::from_vec_with_nul(file_name_buffer)
                    .unwrap()
                    .to_string_lossy();
                Ok(ModuleInfo2 {
                    base_load_address,
                    file_name,
                    assembly_id,
                    module_flags,
                })
            }
            _ => Err(hr),
        }
    }

    pub fn get_runtime_information(&self) -> Result<RuntimeInfo, HRESULT> {
        let mut version_string_buffer_length = MaybeUninit::uninit();
        unsafe {
            self.GetRuntimeInformation(
                ptr::null_mut(),
                ptr::null_mut(),
                ptr::null_mut(),
                ptr::null_mut(),
                ptr::null_mut(),
                ptr::null_mut(),
                0,
                version_string_buffer_length.as_mut_ptr(),
                ptr::null_mut(),
            )
        };

        let version_string_buffer_length = unsafe { version_string_buffer_length.assume_init() };
        let mut version_string_buffer =
            Vec::<WCHAR>::with_capacity(version_string_buffer_length as usize);
        unsafe { version_string_buffer.set_len(version_string_buffer_length as usize) };

        let mut clr_instance_id = MaybeUninit::uninit();
        let mut runtime_type = MaybeUninit::uninit();
        let mut major_version = MaybeUninit::uninit();
        let mut minor_version = MaybeUninit::uninit();
        let mut build_number = MaybeUninit::uninit();
        let mut qfe_version = MaybeUninit::uninit();
        let mut version_string_length = MaybeUninit::uninit();
        let hr = unsafe {
            self.GetRuntimeInformation(
                clr_instance_id.as_mut_ptr(),
                runtime_type.as_mut_ptr(),
                major_version.as_mut_ptr(),
                minor_version.as_mut_ptr(),
                build_number.as_mut_ptr(),
                qfe_version.as_mut_ptr(),
                version_string_buffer_length,
                version_string_length.as_mut_ptr(),
                version_string_buffer.as_mut_ptr(),
            )
        };

        match hr {
            S_OK => {
                let clr_instance_id = unsafe { clr_instance_id.assume_init() };
                let runtime_type = unsafe { runtime_type.assume_init() };
                let major_version = unsafe { major_version.assume_init() };
                let minor_version = unsafe { minor_version.assume_init() };
                let build_number = unsafe { build_number.assume_init() };
                let qfe_version = unsafe { qfe_version.assume_init() };
                let version_string = U16CString::from_vec_with_nul(version_string_buffer)
                    .unwrap()
                    .to_string_lossy();
                Ok(RuntimeInfo {
                    clr_instance_id,
                    runtime_type,
                    major_version,
                    minor_version,
                    build_number,
                    qfe_version,
                    version_string,
                })
            }
            _ => Err(hr),
        }
    }
}

impl ICorProfilerInfo4 {
    pub fn initialize_current_thread(&self) -> Result<(), HRESULT> {
        let hr = unsafe { self.InitializeCurrentThread() };
        match hr {
            S_OK => Ok(()),
            _ => Err(hr),
        }
    }

    pub fn request_rejit(
        &self,
        module_ids: &[ModuleID],
        method_ids: &[mdMethodDef],
    ) -> Result<(), HRESULT> {
        let len = method_ids.len() as ULONG;
        let hr = unsafe { self.RequestReJIT(len, module_ids.as_ptr(), method_ids.as_ptr()) };
        match hr {
            S_OK => Ok(()),
            _ => Err(hr),
        }
    }
}

// allow it to be moved to another thread for rejitting.
// We know this is safe to perform, but compiler doesn't
unsafe impl Send for ICorProfilerInfo4 {}

impl ICorProfilerInfo5 {
    pub fn set_event_mask2(
        &self,
        events_low: COR_PRF_MONITOR,
        events_high: COR_PRF_HIGH_MONITOR,
    ) -> Result<(), HRESULT> {
        let events_low = events_low.bits();
        let events_high = events_high.bits();
        let hr = unsafe { self.SetEventMask2(events_low, events_high) };
        match hr {
            S_OK => Ok(()),
            _ => Err(hr),
        }
    }
}

impl ICorProfilerInfo7 {
    pub fn apply_metadata(&self, module_id: ModuleID) -> Result<(), HRESULT> {
        let hr = unsafe { self.ApplyMetaData(module_id) };
        match hr {
            S_OK => Ok(()),
            _ => Err(hr),
        }
    }
}
