use crate::{
    cli::{Method, Operand},
    ffi::*,
    interfaces::{
        icor_profiler_assembly_reference_provider::ICorProfilerAssemblyReferenceProvider,
        icor_profiler_callback::{
            ICorProfilerCallback, ICorProfilerCallback2, ICorProfilerCallback3,
            ICorProfilerCallback4, ICorProfilerCallback5, ICorProfilerCallback6,
            ICorProfilerCallback7, ICorProfilerCallback8, ICorProfilerCallback9,
        },
        icor_profiler_function_control::ICorProfilerFunctionControl,
        icor_profiler_info::{
            ICorProfilerInfo, ICorProfilerInfo2, ICorProfilerInfo3, ICorProfilerInfo4,
            ICorProfilerInfo5, ICorProfilerInfo7, IID_ICOR_PROFILER_INFO, IID_ICOR_PROFILER_INFO4,
        },
        imetadata_assembly_import::IMetaDataAssemblyImport,
        imetadata_emit::{IMetaDataEmit, IMetaDataEmit2},
        imetadata_import::{IMetaDataImport, IMetaDataImport2},
    },
    profiler::managed::ManagedLoader,
    types::{HashAlgorithmType, ModuleInfo,Version,RuntimeInfo},
};
use com::{
    interfaces::iunknown::IUnknown,
    sys::{FAILED, GUID, HRESULT},
    Interface,
};
use rust_embed::RustEmbed;
use simple_logger::SimpleLogger;
use std::{
    cell::RefCell,
    ffi::c_void,
    sync::{atomic::AtomicBool, Mutex},
    collections::HashMap,
};
use std::sync::atomic::{AtomicUsize, Ordering};
use crate::cli::rem_un;
use crate::profiler::types::{IntegrationMethod, Integration};
use std::ops::Deref;
use std::fs::File;
use std::io::BufReader;

const MANAGED_PROFILER_ASSEMBLY: &'static str = "Elastic.Apm.Profiler.Managed";
const ELASTIC_APM_PROFILER_INTEGRATIONS: &'static str = "ELASTIC_APM_PROFILER_INTEGRATIONS";
const ELASTIC_APM_PROFILER_CALLTARGET_ENABLED: &'static str = "ELASTIC_APM_PROFILER_CALLTARGET_ENABLED";

lazy_static! {
    static ref LOCK: Mutex<i32> = Mutex::new(0);
    static ref MODULES: Mutex<HashMap<ModuleID, crate::profiler::types::ModuleInfo>> = Mutex::new(HashMap::new());
    static ref FIRST_JIT_COMPILATION_APP_DOMAINS: Mutex<Vec<AppDomainID>> = Mutex::new(Vec::new());
    static ref MANAGED_PROFILER_LOADED_APP_DOMAINS: Mutex<Vec<AppDomainID>> = Mutex::new(Vec::new());

    static ref SKIP_ASSEMBLY_PREFIXES: Vec<&'static str> = vec![
        "Elastic.Apm",
        "MessagePack",
        "Microsoft.AI",
        "Microsoft.ApplicationInsights",
        "Microsoft.Build",
        "Microsoft.CSharp",
        "Microsoft.Extensions",
        "Microsoft.Web.Compilation.Snapshots",
        "Sigil",
        "System.Core",
        "System.Console",
        "System.Collections",
        "System.ComponentModel",
        "System.Diagnostics",
        "System.Drawing",
        "System.EnterpriseServices",
        "System.IO",
        "System.Runtime",
        "System.Text",
        "System.Threading",
        "System.Xml",
        "Newtonsoft",
    ];

    static ref SKIP_ASSEMBLIES: Vec<&'static str> = vec![
        "mscorlib",
        "netstandard",
        "System.Configuration",
        "Microsoft.AspNetCore.Razor.Language",
        "Microsoft.AspNetCore.Mvc.RazorPages",
        "Anonymously Hosted DynamicMethods Assembly",
        "ISymWrapper",
    ];

    static ref PROFILER_VERSION: Version = Version::new(1,9,0,0);

    static ref INTEGRATION_METHODS: Mutex<Vec<IntegrationMethod>> = Mutex::new(Vec::new());
}

static COR_LIB_MODULE_LOADED: AtomicBool = AtomicBool::new(false);
static COR_APP_DOMAIN_ID: AtomicUsize = AtomicUsize::new(0);
static MANAGED_PROFILER_LOADED_DOMAIN_NEUTRAL: AtomicBool = AtomicBool::new(false);

pub(crate) static IS_ATTACHED: AtomicBool = AtomicBool::new(false);

class! {
    /// The profiler implementation
    pub class CorProfiler:
        ICorProfilerCallback9(ICorProfilerCallback8(ICorProfilerCallback7(
            ICorProfilerCallback6(ICorProfilerCallback5(ICorProfilerCallback4(
                ICorProfilerCallback3(ICorProfilerCallback2(ICorProfilerCallback)))))))) {
        profiler_info: RefCell<Option<ICorProfilerInfo4>>,
        runtime_info: RefCell<Option<RuntimeInfo>>,
    }

    impl ICorProfilerCallback for CorProfiler {
        pub fn Initialize(
             &self,
            pICorProfilerInfoUnk: IUnknown,
        ) -> HRESULT {
            match self.initialize(pICorProfilerInfoUnk) {
                Ok(_) => S_OK,
                Err(_) => S_OK
            }
        }
        pub fn Shutdown(&self) -> HRESULT {
            match self.shutdown() {
                Ok(_) => S_OK,
                Err(_) => S_OK
            }
        }
        pub fn AppDomainCreationStarted(&self, appDomainId: AppDomainID) -> HRESULT { S_OK }
        pub fn AppDomainCreationFinished(
             &self,
            appDomainId: AppDomainID,
            hrStatus: HRESULT,
        ) -> HRESULT { S_OK }
        pub fn AppDomainShutdownStarted(&self, appDomainId: AppDomainID) -> HRESULT { S_OK }
        pub fn AppDomainShutdownFinished(
             &self,
            appDomainId: AppDomainID,
            hrStatus: HRESULT,
        ) -> HRESULT { S_OK }
        pub fn AssemblyLoadStarted(&self, assemblyId: AssemblyID) -> HRESULT { S_OK }
        pub fn AssemblyLoadFinished(
            &self,
            assemblyId: AssemblyID,
            hrStatus: HRESULT,
        ) -> HRESULT {
            match self.assembly_load_finished(assemblyId, hrStatus) {
                Ok(_) => S_OK,
                Err(_) => S_OK,
            }
        }
        pub fn AssemblyUnloadStarted(&self, assemblyId: AssemblyID) -> HRESULT { S_OK }
        pub fn AssemblyUnloadFinished(
             &self,
            assemblyId: AssemblyID,
            hrStatus: HRESULT,
        ) -> HRESULT { S_OK }
        pub fn ModuleLoadStarted(&self, moduleId: ModuleID) -> HRESULT { S_OK }
        pub fn ModuleLoadFinished(&self, moduleId: ModuleID, hrStatus: HRESULT) -> HRESULT {
            match self.module_load_finished(moduleId, hrStatus) {
                Ok(_) => S_OK,
                Err(_) => S_OK,
            }
        }
        pub fn ModuleUnloadStarted(&self, moduleId: ModuleID) -> HRESULT { S_OK }
        pub fn ModuleUnloadFinished(&self, moduleId: ModuleID, hrStatus: HRESULT) -> HRESULT { S_OK }
        pub fn ModuleAttachedToAssembly(
             &self,
            moduleId: ModuleID,
            AssemblyId: AssemblyID,
        ) -> HRESULT { S_OK }
        pub fn ClassLoadStarted(&self, classId: ClassID) -> HRESULT { S_OK }
        pub fn ClassLoadFinished(&self, classId: ClassID, hrStatus: HRESULT) -> HRESULT { S_OK }
        pub fn ClassUnloadStarted(&self, classId: ClassID) -> HRESULT { S_OK }
        pub fn ClassUnloadFinished(&self, classId: ClassID, hrStatus: HRESULT) -> HRESULT { S_OK }
        pub fn FunctionUnloadStarted(&self, functionId: FunctionID) -> HRESULT { S_OK }
        pub fn JITCompilationStarted(
             &self,
            functionId: FunctionID,
            fIsSafeToBlock: BOOL,
        ) -> HRESULT {
            match self.jit_compilation_started(functionId, fIsSafeToBlock) {
                Ok(_) => S_OK,
                Err(_) => S_OK,
            }
        }
        pub fn JITCompilationFinished(
             &self,
            functionId: FunctionID,
            hrStatus: HRESULT,
            fIsSafeToBlock: BOOL,
        ) -> HRESULT { S_OK }
        pub fn JITCachedFunctionSearchStarted(
             &self,
            functionId: FunctionID,
            pbUseCachedFunction: *mut BOOL,
        ) -> HRESULT { S_OK }
        pub fn JITCachedFunctionSearchFinished(
             &self,
            functionId: FunctionID,
            result: COR_PRF_JIT_CACHE,
        ) -> HRESULT { S_OK }
        pub fn JITFunctionPitched(&self, functionId: FunctionID) -> HRESULT { S_OK }
        pub fn JITInlining(
             &self,
            callerId: FunctionID,
            calleeId: FunctionID,
            pfShouldInline: *mut BOOL,
        ) -> HRESULT { S_OK }
        pub fn ThreadCreated(&self, threadId: ThreadID) -> HRESULT { S_OK }
        pub fn ThreadDestroyed(&self, threadId: ThreadID) -> HRESULT { S_OK }
        pub fn ThreadAssignedToOSThread(
             &self,
            managedThreadId: ThreadID,
            osThreadId: DWORD,
        ) -> HRESULT { S_OK }
        pub fn RemotingClientInvocationStarted(&self) -> HRESULT { S_OK }
        pub fn RemotingClientSendingMessage(&self, pCookie: *const GUID, fIsAsync: BOOL) -> HRESULT { S_OK }
        pub fn RemotingClientReceivingReply(&self, pCookie: *const GUID, fIsAsync: BOOL) -> HRESULT { S_OK }
        pub fn RemotingClientInvocationFinished(&self) -> HRESULT { S_OK }
        pub fn RemotingServerReceivingMessage(&self, pCookie: *const GUID, fIsAsync: BOOL) -> HRESULT { S_OK }
        pub fn RemotingServerInvocationStarted(&self) -> HRESULT { S_OK }
        pub fn RemotingServerInvocationReturned(&self) -> HRESULT { S_OK }
        pub fn RemotingServerSendingReply(&self, pCookie: *const GUID, fIsAsync: BOOL) -> HRESULT { S_OK }
        pub fn UnmanagedToManagedTransition(
             &self,
            functionId: FunctionID,
            reason: COR_PRF_TRANSITION_REASON,
        ) -> HRESULT { S_OK }
        pub fn ManagedToUnmanagedTransition(
             &self,
            functionId: FunctionID,
            reason: COR_PRF_TRANSITION_REASON,
        ) -> HRESULT { S_OK }
        pub fn RuntimeSuspendStarted(&self, suspendReason: COR_PRF_SUSPEND_REASON) -> HRESULT { S_OK }
        pub fn RuntimeSuspendFinished(&self) -> HRESULT { S_OK }
        pub fn RuntimeSuspendAborted(&self) -> HRESULT { S_OK }
        pub fn RuntimeResumeStarted(&self) -> HRESULT { S_OK }
        pub fn RuntimeResumeFinished(&self) -> HRESULT { S_OK }
        pub fn RuntimeThreadSuspended(&self, threadId: ThreadID) -> HRESULT { S_OK }
        pub fn RuntimeThreadResumed(&self, threadId: ThreadID) -> HRESULT { S_OK }
        pub fn MovedReferences(
             &self,
            cMovedObjectIDRanges: ULONG,
            oldObjectIDRangeStart: *const ObjectID,
            newObjectIDRangeStart: *const ObjectID,
            cObjectIDRangeLength: *const ULONG,
        ) -> HRESULT { S_OK }
        pub fn ObjectAllocated(&self, objectId: ObjectID, classId: ClassID) -> HRESULT { S_OK }
        pub fn ObjectsAllocatedByClass(
             &self,
            cClassCount: ULONG,
            classIds: *const ClassID,
            cObjects: *const ULONG,
        ) -> HRESULT { S_OK }
        pub fn ObjectReferences(
             &self,
            objectId: ObjectID,
            classId: ClassID,
            cObjectRefs: ULONG,
            objectRefIds: *const ObjectID,
        ) -> HRESULT { S_OK }
        pub fn RootReferences(
             &self,
            cRootRefs: ULONG,
            rootRefIds: *const ObjectID,
        ) -> HRESULT { S_OK }
        pub fn ExceptionThrown(&self, thrownObjectId: ObjectID) -> HRESULT { S_OK }
        pub fn ExceptionSearchFunctionEnter(&self, functionId: FunctionID) -> HRESULT { S_OK }
        pub fn ExceptionSearchFunctionLeave(&self) -> HRESULT { S_OK }
        pub fn ExceptionSearchFilterEnter(&self, functionId: FunctionID) -> HRESULT { S_OK }
        pub fn ExceptionSearchFilterLeave(&self) -> HRESULT { S_OK }
        pub fn ExceptionSearchCatcherFound(&self, functionId: FunctionID) -> HRESULT { S_OK }
        pub fn ExceptionOSHandlerEnter(&self, __unused: UINT_PTR) -> HRESULT { S_OK }
        pub fn ExceptionOSHandlerLeave(&self, __unused: UINT_PTR) -> HRESULT { S_OK }
        pub fn ExceptionUnwindFunctionEnter(&self, functionId: FunctionID) -> HRESULT { S_OK }
        pub fn ExceptionUnwindFunctionLeave(&self) -> HRESULT { S_OK }
        pub fn ExceptionUnwindFinallyEnter(&self, functionId: FunctionID) -> HRESULT { S_OK }
        pub fn ExceptionUnwindFinallyLeave(&self) -> HRESULT { S_OK }
        pub fn ExceptionCatcherEnter(
             &self,
            functionId: FunctionID,
            objectId: ObjectID,
        ) -> HRESULT { S_OK }
        pub fn ExceptionCatcherLeave(&self) -> HRESULT { S_OK }
        pub fn COMClassicVTableCreated(
             &self,
            wrappedClassId: ClassID,
            implementedIID: REFGUID,
            pVTable: *const c_void,
            cSlots: ULONG,
        ) -> HRESULT { S_OK }
        pub fn COMClassicVTableDestroyed(
             &self,
            wrappedClassId: ClassID,
            implementedIID: REFGUID,
            pVTable: *const c_void,
        ) -> HRESULT { S_OK }
        pub fn ExceptionCLRCatcherFound(&self) -> HRESULT { S_OK }
        pub fn ExceptionCLRCatcherExecute(&self) -> HRESULT { S_OK }
    }

    impl ICorProfilerCallback2 for CorProfiler {
        pub fn ThreadNameChanged(&self,
            threadId: ThreadID,
            cchName: ULONG,
            name: *const WCHAR,
        ) -> HRESULT { S_OK }
        pub fn GarbageCollectionStarted(&self,
            cGenerations: int,
            generationCollected: *const BOOL,
            reason: COR_PRF_GC_REASON,
        ) -> HRESULT { S_OK }
        pub fn SurvivingReferences(&self,
            cSurvivingObjectIDRanges: ULONG,
            objectIDRangeStart: *const ObjectID,
            cObjectIDRangeLength: *const ULONG,
        ) -> HRESULT { S_OK }
        pub fn GarbageCollectionFinished(&self) -> HRESULT { S_OK }
        pub fn FinalizeableObjectQueued(&self,
            finalizerFlags: DWORD,
            objectID: ObjectID,
        ) -> HRESULT { S_OK }
        pub fn RootReferences2(&self,
            cRootRefs: ULONG,
            rootRefIds: *const ObjectID,
            rootKinds: *const COR_PRF_GC_ROOT_KIND,
            rootFlags: *const COR_PRF_GC_ROOT_FLAGS,
            rootIds: *const UINT_PTR,
        ) -> HRESULT { S_OK }
        pub fn HandleCreated(&self,
            handleId: GCHandleID,
            initialObjectId: ObjectID,
        ) -> HRESULT { S_OK }
        pub fn HandleDestroyed(&self, handleId: GCHandleID) -> HRESULT { S_OK }
    }

    impl ICorProfilerCallback3 for CorProfiler {
        pub fn InitializeForAttach(&self,
            pCorProfilerInfoUnk: *const IUnknown,
            pvClientData: *const c_void,
            cbClientData: UINT,
        ) -> HRESULT { S_OK }
        pub fn ProfilerAttachComplete(&self) -> HRESULT { S_OK }
        pub fn ProfilerDetachSucceeded(&self) -> HRESULT { S_OK }
    }

    impl ICorProfilerCallback4 for CorProfiler {
        pub fn ReJITCompilationStarted(&self,
            functionId: FunctionID,
            rejitId: ReJITID,
            fIsSafeToBlock: BOOL,
        ) -> HRESULT { S_OK }
        pub fn GetReJITParameters(&self,
            moduleId: ModuleID,
            methodId: mdMethodDef,
            pFunctionControl: *const ICorProfilerFunctionControl,
        ) -> HRESULT { S_OK }
        pub fn ReJITCompilationFinished(&self,
            functionId: FunctionID,
            rejitId: ReJITID,
            hrStatus: HRESULT,
            fIsSafeToBlock: BOOL,
        ) -> HRESULT { S_OK }
        pub fn ReJITError(&self,
            moduleId: ModuleID,
            methodId: mdMethodDef,
            functionId: FunctionID,
            hrStatus: HRESULT,
        ) -> HRESULT { S_OK }
        pub fn MovedReferences2(&self,
            cMovedObjectIDRanges: ULONG,
            oldObjectIDRangeStart: *const ObjectID,
            newObjectIDRangeStart: *const ObjectID,
            cObjectIDRangeLength: *const SIZE_T,
        ) -> HRESULT { S_OK }
        pub fn SurvivingReferences2(&self,
            cSurvivingObjectIDRanges: ULONG,
            objectIDRangeStart: *const ObjectID,
            cObjectIDRangeLength: *const SIZE_T,
        ) -> HRESULT { S_OK }
    }

    impl ICorProfilerCallback5 for CorProfiler {
        pub fn ConditionalWeakTableElementReferences(&self,
            cRootRefs: ULONG,
            keyRefIds: *const ObjectID,
            valueRefIds: *const ObjectID,
            rootIds: *const GCHandleID,
        ) -> HRESULT { S_OK }
    }

    impl ICorProfilerCallback6 for CorProfiler {
        pub fn GetAssemblyReferences(&self,
            wszAssemblyPath: *const WCHAR,
            pAsmRefProvider: *const ICorProfilerAssemblyReferenceProvider,
        ) -> HRESULT { S_OK }
    }

    impl ICorProfilerCallback7 for CorProfiler {
        pub fn ModuleInMemorySymbolsUpdated(&self, moduleId: ModuleID) -> HRESULT { S_OK }
    }

    impl ICorProfilerCallback8 for CorProfiler {
        pub fn DynamicMethodJITCompilationStarted(&self,
            functionId: FunctionID,
            fIsSafeToBlock: BOOL,
            pILHeader: LPCBYTE,
            cbILHeader: ULONG,
        ) -> HRESULT { S_OK }
        pub fn DynamicMethodJITCompilationFinished(&self,
            functionId: FunctionID,
            hrStatus: HRESULT,
            fIsSafeToBlock: BOOL,
        ) -> HRESULT { S_OK }
    }

    impl ICorProfilerCallback9 for CorProfiler {
        pub fn DynamicMethodUnloaded(&self, functionId: FunctionID) -> HRESULT { S_OK }
    }
}

impl CorProfiler {
    fn initialize(&self, unknown: IUnknown) -> Result<(), HRESULT> {
        let _lock = LOCK.lock().unwrap();

        SimpleLogger::from_env().init().unwrap();
        log::trace!("initialize");

        let integrations = self.load_integrations()?;

        // get the ICorProfilerInfo4 interface, which will be available for all CLR versions targeted
        let profiler_info = unknown.query_interface::<ICorProfilerInfo4>();
        if profiler_info.is_none() {
            log::error!("could not get ICorProfilerInfo4 from IUnknown");
            return Err(E_FAIL);
        }

        let profiler_info = profiler_info.unwrap();

        // get the integrations from file
        let integrations: Vec<Integration> = self.load_integrations()?;
        let calltarget_enabled = self.read_calltarget_env_var();

        if calltarget_enabled {
            // TODO: initialize rejit handler
        }
        
        let mut integration_methods: Vec<IntegrationMethod> = integrations
            .iter()
            .flat_map(|i| i.method_replacements.iter().filter_map(move |m| {
                if let Some(wrapper_method) = &m.wrapper {
                    let is_calltarget = &wrapper_method.action == "CallTargetModification";
                    if calltarget_enabled && is_calltarget {
                        Some(IntegrationMethod { name: i.name.clone(), method_replacement: m.clone() })
                    } else if !calltarget_enabled && !is_calltarget {
                        Some(IntegrationMethod { name: i.name.clone(), method_replacement: m.clone() })
                    } else {
                        None
                    }
                } else {
                    None
                }
            }))
            .collect();

        if integration_methods.is_empty() {
            log::warn!("Initialize: No integrations loaded. Profiler disabled.");
            return Err(E_FAIL);
        } else {
            log::debug!("Initialize: loaded {} integration(s)", integration_methods.len());
        }

        INTEGRATION_METHODS.lock().unwrap().append(&mut integration_methods);

        // Set the event mask for CLR events we're interested in
        let mut event_mask = COR_PRF_MONITOR::COR_PRF_MONITOR_JIT_COMPILATION
            | COR_PRF_MONITOR::COR_PRF_DISABLE_TRANSPARENCY_CHECKS_UNDER_FULL_TRUST
            | COR_PRF_MONITOR::COR_PRF_MONITOR_MODULE_LOADS
            | COR_PRF_MONITOR::COR_PRF_MONITOR_ASSEMBLY_LOADS
            | COR_PRF_MONITOR::COR_PRF_DISABLE_ALL_NGEN_IMAGES;

        if calltarget_enabled {
            log::info!("CallTarget instrumentation is enabled");
            event_mask |= COR_PRF_MONITOR::COR_PRF_ENABLE_REJIT;
        } else {
            log::info!("CallTarget instrumentation is disabled");
        }

        // TODO: enable/disable inlining and optimizations

        log::trace!("set event mask to {:?}", &event_mask);
        profiler_info.set_event_mask(event_mask)?;

        // if the runtime also supports ICorProfilerInfo5, set eventmask2
        if let Some(profiler_info5) = unknown.query_interface::<ICorProfilerInfo5>() {
            let event_mask2 = COR_PRF_HIGH_MONITOR::COR_PRF_HIGH_ADD_ASSEMBLY_REFERENCES;
            log::trace!("set event mask2 to {:?}", &event_mask2);
            profiler_info5.set_event_mask2(event_mask, event_mask2)?;
        }

        // get the details for the runtime
        let runtime_info = profiler_info.get_runtime_information()?;
        log::info!("runtime {}", &runtime_info);

        // Store the profiler and runtime info for later use
        self.profiler_info.replace(Some(profiler_info));
        self.runtime_info.replace(Some(runtime_info));

        IS_ATTACHED.store(true, Ordering::SeqCst);

        Ok(())
    }

    fn shutdown(&self) -> Result<(), HRESULT> {
        log::trace!("shutdown");
        let _ = LOCK.lock().unwrap();

        self.profiler_info.replace(None);
        IS_ATTACHED.store(false, Ordering::SeqCst);

        Ok(())
    }

    fn assembly_load_finished(
        &self,
        assembly_id: AssemblyID,
        hr_status: HRESULT,
    ) -> Result<(), HRESULT> {
        if FAILED(hr_status) {
            log::error!("hrStatus is {:X}", hr_status);
            return Ok(());
        }

        if !IS_ATTACHED.load(Ordering::SeqCst) {
            log::trace!("profiler not attached");
            return Ok(());
        }

        let _lock = LOCK.lock().unwrap();
        if !IS_ATTACHED.load(Ordering::SeqCst) {
            log::trace!("profiler not attached");
            return Ok(());
        }

        let borrow = self.profiler_info.borrow();
        let profiler_info = borrow.as_ref().unwrap();

        let assembly_info = profiler_info.get_assembly_info(assembly_id)?;
        let metadata_import = profiler_info.get_module_metadata::<IMetaDataImport2>(
            assembly_info.module_id, CorOpenFlags::ofRead)?;

        let metadata_assembly_import = metadata_import
            .query_interface::<IMetaDataAssemblyImport>()
            .expect("unable to get meta data assembly import");

        let assembly_metadata = metadata_assembly_import.get_assembly_metadata()?;

        let is_managed_profiler_assembly = &assembly_info.name == MANAGED_PROFILER_ASSEMBLY;

        log::debug!("AssemblyLoadFinished: name={}, version={}", &assembly_metadata.name, &assembly_metadata.version);
        if is_managed_profiler_assembly {
            if &assembly_metadata.version == PROFILER_VERSION.deref() {
                log::info!(
                    "AssemblyLoadFinished: {} {} matched profiler version {}",
                    MANAGED_PROFILER_ASSEMBLY,
                    &assembly_metadata.version,
                    PROFILER_VERSION.deref());

                MANAGED_PROFILER_LOADED_APP_DOMAINS.lock().unwrap().push(assembly_info.app_domain_id);

                let runtime_borrow = self.runtime_info.borrow();
                let runtime_info = runtime_borrow.as_ref().unwrap();

                if runtime_info.is_desktop_clr() && COR_LIB_MODULE_LOADED.load(Ordering::SeqCst) {
                    if assembly_info.app_domain_id == COR_APP_DOMAIN_ID.load(Ordering::SeqCst) {
                        log::info!("AssemblyLoadFinished: {} was loaded domain-neutral", MANAGED_PROFILER_ASSEMBLY);
                        MANAGED_PROFILER_LOADED_DOMAIN_NEUTRAL.store(true, Ordering::SeqCst);
                    } else {
                        log::info!("AssemblyLoadFinished: {} was not loaded domain-neutral", MANAGED_PROFILER_ASSEMBLY);
                    }
                }


            } else {
                log::warn!("AssemblyLoadFinished: {} {} did not match profiler version {}",
                           MANAGED_PROFILER_ASSEMBLY,
                           &assembly_metadata.version,
                           PROFILER_VERSION.deref());
            }
        }

        Ok(())
    }

    fn module_load_finished(&self, module_id: ModuleID, hr_status: HRESULT) -> Result<(), HRESULT> {
        if FAILED(hr_status) {
            log::error!("hr status is {} for module id {}. skipping", hr_status, module_id);
            return Ok(());
        }

        let _lock = LOCK.lock().unwrap();

        if let Some(module_info) = self.get_module_info(module_id) {
            let appdomain_id = module_info.assembly.app_domain_id;
            let assembly_name = &module_info.assembly.name;

            log::debug!(
                "Module loaded {}, {}, app domain {}",
                &module_info.path,
                assembly_name,
                &module_info.assembly.app_domain_name
            );

            if !COR_LIB_MODULE_LOADED.load(Ordering::Relaxed) &&
                assembly_name == "mscorlib" || assembly_name == "System.Private.CoreLib" {
                COR_LIB_MODULE_LOADED.store(true, Ordering::Relaxed);
                COR_APP_DOMAIN_ID.store(appdomain_id, Ordering::Relaxed);
                return Ok(());
            }

            if assembly_name == "Elastic.Apm.Profiler.Managed.Loader" {
                log::info!(
                    "ModuleLoadFinished: Elastic.Apm.Profiler.Managed.Loader loaded into AppDomain {} {}",
                    appdomain_id,
                    &module_info.assembly.app_domain_name);
                FIRST_JIT_COMPILATION_APP_DOMAINS.lock().unwrap().push(appdomain_id);
            }

            if module_info.is_windows_runtime() {
                log::debug!("skipping windows metadata module {}", module_id);
                return Ok(());
            }

            for pattern in SKIP_ASSEMBLY_PREFIXES.iter() {
               if assembly_name.starts_with(pattern) {
                   log::debug!("skipping module {} {} because it matches skip pattern {}", assembly_name, module_id, pattern);
                   return Ok(());
               }
            }

            for skip in SKIP_ASSEMBLIES.iter() {
                if assembly_name == skip {
                    log::debug!("skipping module {} {} because it matches skip {}", assembly_name, module_id, skip);
                    return Ok(());
                }
            }

            // TODO: filter integrations


            let mut modules = MODULES.lock().unwrap();
            modules.insert(module_id, module_info);

            log::trace!("Tracking {} modules", modules.len());
        }

        Ok(())
    }

    fn jit_compilation_started(
        &self,
        function_id: FunctionID,
        is_safe_to_block: BOOL,
    ) -> Result<(), HRESULT> {
        let _lock = LOCK.lock().unwrap();

        let borrow = self.profiler_info.borrow();
        let profiler_info = borrow.as_ref().unwrap();
        let function_info = profiler_info.get_function_info(function_id)?;

        let metadata_import = profiler_info.get_module_metadata::<IMetaDataImport2>(
            function_info.module_id,
            CorOpenFlags::ofRead | CorOpenFlags::ofWrite,
        )?;

        let method_props = metadata_import.get_method_props(function_info.token)?;
        let type_def_props = metadata_import.get_type_def_props(method_props.class_token)?;

        //log::trace!("jit_compilation_started for {}.{}", &type_def_props.name, &method_props.name);

        if method_props.name == "Greeting" {
            log::trace!("get il function body for Greeting");

            let il_body =
                profiler_info.get_il_function_body(function_info.module_id, function_info.token)?;

            let slice = unsafe {
                std::slice::from_raw_parts(il_body.method_header, il_body.method_size as usize)
            };

            log::trace!("original bytes {:?}", &slice);

            let mut method = Method::new(il_body.method_header, il_body.method_size).unwrap();

            let metadata_emit = metadata_import.query_interface::<IMetaDataEmit2>().unwrap();
            match metadata_emit.define_user_string("Goodbye World!") {
                Ok(md_string) => {
                    log::trace!("Defined user string");

                    let result = metadata_import.get_user_string(md_string);
                    if result.is_err() {
                        log::trace!("Could not read user string {}", md_string);
                        return Ok(());
                    } else {
                        log::trace!("retrieved string '{}'", result.unwrap());
                    }

                    for instr in method.instructions.iter_mut() {
                        if instr.opcode.name == "ldstr" {
                            log::trace!("Found ldstr instruction");
                            match instr.operand {
                                Operand::InlineString(t) => {
                                    let str = metadata_import.get_user_string(t)?;
                                    log::trace!("user string is '{}'", &str);
                                    if str == "Hello World!" {
                                        log::trace!("replaced operand");
                                        instr.operand = Operand::InlineString(md_string);
                                    }
                                }
                                _ => {}
                            }
                        }
                    }

                    let new_method = method.into_bytes();
                    log::trace!("rewritten bytes {:?}", &new_method);
                    log::trace!("get il allocator");
                    let allocator =
                        profiler_info.get_il_function_body_allocator(function_info.module_id)?;
                    log::trace!("get allocate {} bytes", new_method.len());
                    let allocated_bytes = allocator.alloc(new_method.len() as ULONG)?;
                    log::trace!("copy new_method into allocated bytes");

                    let address = unsafe { allocated_bytes.into_inner() };
                    unsafe {
                        std::ptr::copy(new_method.as_ptr(), address, new_method.len());
                    }

                    log::trace!("set il_function_body");
                    profiler_info.set_il_function_body(
                        function_info.module_id,
                        function_info.token,
                        address as *const _,
                    )?;

                    if let Some(profiler_info7) =
                        profiler_info.query_interface::<ICorProfilerInfo7>()
                    {
                        profiler_info7
                            .apply_metadata(function_info.module_id)
                            .unwrap();
                    }
                }
                Err(_) => {}
            }

            let op_codes = method
                .instructions
                .iter()
                .map(|instr| instr.opcode.name)
                .collect::<Vec<_>>()
                .join(",");

            log::info!("method: {}, op_codes: {}", method_props.name, op_codes);
        }

        Ok(())
    }

    fn get_module_info(&self, module_id: ModuleID) -> Option<super::types::ModuleInfo> {
        let borrow = self.profiler_info.borrow();
        let profiler_info = borrow.as_ref().unwrap();
        if let Some(module_info) = profiler_info.get_module_info_2(module_id).ok() {
            if let Some(assembly_info) = profiler_info
                .get_assembly_info(module_info.assembly_id)
                .ok()
            {
                if let Some(app_domain_info) = profiler_info
                    .get_app_domain_info(assembly_info.app_domain_id)
                    .ok()
                {
                    return Some(super::types::ModuleInfo {
                        id: module_id,
                        assembly: super::types::AssemblyInfo {
                            id: module_info.assembly_id,
                            name: assembly_info.name,
                            app_domain_id: assembly_info.app_domain_id,
                            app_domain_name: app_domain_info.name,
                            manifest_module_id: assembly_info.module_id,
                        },
                        path: module_info.file_name,
                        flags: module_info.module_flags,
                    });
                }
            }
        }

        None
    }

    fn load_integrations(&self) -> Result<Vec<Integration>, HRESULT> {
        let path = std::env::var(ELASTIC_APM_PROFILER_INTEGRATIONS).map_err(|e| {
            log::warn!("Problem reading {} environment variable: {}. profiler is disabled.", ELASTIC_APM_PROFILER_INTEGRATIONS, e.to_string());
            E_FAIL
        })?;

        let file = File::open(&path).map_err(|e| {
            log::warn!("Problem reading integrations file {}: {}. profiler is disabled.", &path, e.to_string());
            E_FAIL
        })?;

        let reader = BufReader::new(file);
        let integrations = serde_json::from_reader(reader).map_err(|e| {
            log::warn!("Problem reading integrations file {}: {}. profiler is disabled.", &path, e.to_string());
            E_FAIL
        })?;

        Ok(integrations)
    }

    fn read_calltarget_env_var(&self) -> bool {
        match std::env::var(ELASTIC_APM_PROFILER_CALLTARGET_ENABLED) {
            Ok(enabled) => {
                match enabled.as_str() {
                    "true" => true,
                    "True" => true,
                    "TRUE" => true,
                    "1" => true,
                    "false" => false,
                    "False" => false,
                    "FALSE" => false,
                    "0" => false,
                    _ => true
                }
            },
            Err(e) => {
                log::info!("Problem reading {}: {}. Setting to true", ELASTIC_APM_PROFILER_CALLTARGET_ENABLED, e.to_string());
                true
            }
        }
    }
}
