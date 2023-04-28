use std::process::Command;

fn main() {
    static_vcruntime::metabuild();

    let git_rev_parse = Command::new("git")
        .args(&["rev-parse", "HEAD"])
        .output()
        .unwrap();

    let git_hash = String::from_utf8(git_rev_parse.stdout).unwrap();
    println!("cargo:rustc-env=GIT_HASH={}", git_hash);

    let minver = Command::new("dotnet")
        .args(&["minver", "-t=v", "-p=canary.0", "-v=e"])
        .output()
        .unwrap();

    let version = String::from_utf8(minver.stdout).unwrap();
    println!("cargo:rustc-env=CARGO_PKG_VERSION={}", version);
    println!("cargo:warning=CARGO_PKG_VERSION={}", version);
}
