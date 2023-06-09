use std::process::Command;
use semver::{Version};

fn main() {
    static_vcruntime::metabuild();

    let git_rev_parse = Command::new("git")
        .args(&["rev-parse", "HEAD"])
        .output()
        .unwrap();

    let git_hash = String::from_utf8(git_rev_parse.stdout).unwrap();
    println!("cargo:rustc-env=GIT_HASH={}", git_hash);

    let _restore  = Command::new("dotnet")
        .args(&["tool", "restore"])
        .output()
        .unwrap();

    let minver = Command::new("dotnet")
        .args(&["minver", "-t=v", "-p=canary.0", "-v=e"])
        .output()
        .unwrap();

    let full_version = String::from_utf8(minver.stdout).unwrap();
    let fv = full_version.trim();
    println!("cargo:rustc-env=CARGO_PKG_VERSION={}", &fv);
    println!("cargo:warning=CARGO_PKG_VERSION={}", &fv);

    let semver = Version::parse(&fv);
    match semver {
        Ok(v) => {
            println!("cargo:rustc-env=CARGO_PKG_VERSION_MAJOR={}", v.major);
            println!("cargo:rustc-env=CARGO_PKG_VERSION_MINOR={}", v.minor);
            // anchoring patch version to 0 because that's what our packages do
            // see: src/Directory.Build.targets
            // TODO: starting 2.0 we want to anchor minor to zero to to follow
            // best practices
            println!("cargo:rustc-env=CARGO_PKG_VERSION_PATCH={}", "0");
        },
        Err(e) => {

            println!("cargo:warning=SemverParsingError={}", e.to_string());
        }
    }

}
