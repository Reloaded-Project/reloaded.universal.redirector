echo "Installing wine"
sudo dpkg --add-architecture i386

# We need to use official wine PPA, ubuntu one is severely outdated
# Add Wine Repo Key
# C:\Users\sewer\Documents\Virtual Machines\Ubuntu 64-bit Desktop

sudo mkdir -pm755 /etc/apt/keyrings
sudo wget -O /etc/apt/keyrings/winehq-archive.key https://dl.winehq.org/wine-builds/winehq.key
sudo wget -NP /etc/apt/sources.list.d/ https://dl.winehq.org/wine-builds/ubuntu/dists/jammy/winehq-jammy.sources

sudo apt update
sudo apt install -y --install-recommends winehq-devel

# We need virtual framebuffer because despite quiet option, dotnet install creates window
sudo apt install -y xvfb

# Remove X Server (only needed for simulation on desktop)
unset DISPLAY

# Need to setup wine-mono (we are on a headless server and prefix creation will show a message box... which we can't answer)
wget https://github.com/madewokherd/wine-mono/releases/download/wine-mono-7.4.1/wine-mono-7.4.1-x86.msi
wine msiexec /i wine-mono-7.4.1-x86.msi /qn

# Installing dotnet48 fixes Core SDK install; probably by some transitive dependency. it's weird
# sudo apt install -y winetricks
# xvfb-run winetricks -q --force dotnet48 corefonts

# Required by .NET 7
winecfg -v win7

echo "Downloading Dotnet"

# Install Native SDK for Cross-Compiling
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
sudo apt update
sudo apt install dotnet-sdk-7.0

# Export
export RELOADEDIIMODS=¬

# Dotnet is borked if we only install SDK for some reason, so we pre-install runtime.
wget https://github.com/Reloaded-Project/dotnet-binaries-for-wine-unit-tests/releases/download/7.0/dotnet-sdk-7.0.202-win-x64.exe
wget https://github.com/Reloaded-Project/dotnet-binaries-for-wine-unit-tests/releases/download/7.0/dotnet-sdk-7.0.202-win-x86.exe
xvfb-run wine dotnet-sdk-7.0.202-win-x64.exe /install /quiet /norestart
xvfb-run wine dotnet-sdk-7.0.202-win-x86.exe /install /quiet /norestart

#wget https://github.com/Reloaded-Project/dotnet-binaries-for-wine-unit-tests/releases/download/7.0/windowsdesktop-runtime-7.0.4-win-x86.exe
#wget https://github.com/Reloaded-Project/dotnet-binaries-for-wine-unit-tests/releases/download/7.0/windowsdesktop-runtime-7.0.4-win-x64.exe
#xvfb-run wine windowsdesktop-runtime-7.0.4-win-x86.exe /install /quiet /norestart
#xvfb-run wine windowsdesktop-runtime-7.0.4-win-x64.exe /install /quiet /norestart

echo "Running Tests"
dotnet build -c Release --runtime win-x64 --no-self-contained ./Reloaded.Universal.Redirector.Tests/Reloaded.Universal.Redirector.Tests.csproj
wine dotnet test -c Release --no-build --no-restore ./Reloaded.Universal.Redirector.sln 
dotnet build -c Release --runtime win-x86 --no-self-contained ./Reloaded.Universal.Redirector.Tests/Reloaded.Universal.Redirector.Tests.csproj
wine dotnet test -c Release --no-build --no-restore ./Reloaded.Universal.Redirector.sln