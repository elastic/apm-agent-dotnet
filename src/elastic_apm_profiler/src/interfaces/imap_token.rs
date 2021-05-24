use crate::ffi::mdToken;
use com::{interfaces::iunknown::IUnknown, sys::HRESULT};

interfaces! {
    #[uuid("06A3EA8B-0225-11d1-BF72-00C04FC31E12")]
    pub unsafe interface IMapToken: IUnknown {
        fn Map(&self, tkImp: mdToken, tkEmit: mdToken) -> HRESULT;
    }
}
