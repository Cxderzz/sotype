Name:           sotype
Version:        0.1.0
Release:        1%{?dist}
Summary:        A full-screen terminal typing test in the style of monkeytype
License:        MIT
URL:            https://github.com/Cxderzz/sotype
# Supplied by the release workflow as a `git archive` of the tagged checkout.
Source0:        %{name}.tar.gz
ExclusiveArch:  x86_64
BuildRequires:  dotnet-sdk-10.0

# Self-contained: not every RPM distro packages a .NET runtime, so the package
# bundles its own. Automatic dependency scanning would pick up optional native
# libraries in that runtime (e.g. lttng-ust for tracing), so list what it really
# needs by soname, which resolves on Fedora, RHEL and openSUSE alike.
AutoReqProv:    no
Requires:       libc.so.6()(64bit)
Requires:       libgcc_s.so.1()(64bit)
Requires:       libstdc++.so.6()(64bit)

# The .NET runtime ships prebuilt and already stripped: skip debuginfo and strip.
%global debug_package %{nil}
%global __os_install_post %{nil}

%description
Live per-character coloring, a smooth caret, timed and word-count modes,
themes, and run history, all in your terminal.

%prep
%setup -q -n %{name}

%build
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
dotnet publish src/Sotype.Console/Sotype.Console.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -p:DebugType=none \
  -o publish

%install
install -d %{buildroot}%{_prefix}/lib/%{name}
cp -r publish/* %{buildroot}%{_prefix}/lib/%{name}/
install -d %{buildroot}%{_bindir}
# The apphost follows this symlink back to /usr/lib/sotype to find its assemblies.
ln -s %{_prefix}/lib/%{name}/Sotype.Console %{buildroot}%{_bindir}/%{name}

%files
%license LICENSE
%{_prefix}/lib/%{name}
%{_bindir}/%{name}
