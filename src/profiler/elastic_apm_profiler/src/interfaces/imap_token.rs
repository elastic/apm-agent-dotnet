// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

use crate::ffi::mdToken;
use com::{interfaces::iunknown::IUnknown, sys::HRESULT};

interfaces! {
    #[uuid("06A3EA8B-0225-11d1-BF72-00C04FC31E12")]
    pub unsafe interface IMapToken: IUnknown {
        fn Map(&self, tkImp: mdToken, tkEmit: mdToken) -> HRESULT;
    }
}
