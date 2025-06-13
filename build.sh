if [ $# != 2 ]; then
   echo "This script can accept two arguments, but only needs one. They are in this order: representing the version to build and whether or not you are running this in an interactive shell. i.e. ./build.sh 4.0.0.0 true"
   read -n1 -r -p "Press any key to exit."
   exit 1
fi

function pub {
  echo "Compiling for $1..."
  dotnet publish -c Release -r $1 --self-contained -o ../../build/$1 -p:Version="$2"
}

function pack {
  cd build/$1
  
  rm appsettings.Development.json
  
  if stringContain "win" $1; then
    mv Ryujinx.Systems.Update.Server.exe ../../artifacts/RyujinxUpdateServer-v$2_$1.exe
  else
    chmod +x Ryujinx.Systems.Update.Server
    mv Ryujinx.Systems.Update.Server ../../artifacts/RyujinxUpdateServer-v$2_$1
  fi
  cd ../../
}

stringContain() { case $2 in *$1* ) return 0;; *) return 1;; esac ;}

echo "Cleaning previous build & packages..."
rm -rf build
rm -rf nuget_build
rm -rf artifacts

mkdir artifacts

echo "Compiling server..."

cd src/Server

pub linux-arm64 $1
pub linux-x64 $1
pub win-x64 $1

cd ../../
echo "Packaging builds..."

pack linux-arm64 $1
pack linux-x64 $1
pack win-x64 $1

echo "Compiling client library..."
cd src/Client
dotnet build -o ../../nuget_build -p:Version="$1"
cd ../../

mv build/linux-x64/appsettings.json artifacts/appsettings.json

echo "Complete. You can find builds for all 3 OSes in build/. Pre-made upload-ready versions are in artifacts/."

if [ $2 != "false" ]; then
  read -n1 -r -p "Press any key to exit."  
fi