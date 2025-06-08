if [ $# != 2 ]; then
   echo "This script can accept two arguments, but only needs one. They are in this order: representing the version to build and whether or not you are running this in an interactive shell. i.e. ./build.sh 4.0.0.0 true"
   read -n1 -r -p "Press any key to exit."
   exit 1
fi

function pub {
  echo "Compiling for $1..."
  dotnet publish -c Release -r $1 --self-contained -o ../build/$1 -p:Version="$2"
}

function pack {
  cd build/$1
  
  rm publish/appsettings.Development.json
  
  if stringContain "win" $1; then
    mv RyujinxUpdate.exe ../../artifacts/RyujinxUpdate-v$2_$1.exe
  else
    chmod +x RyujinxUpdate
    mv RyujinxUpdate ../../artifacts/RyujinxUpdate-v$2_$1
  fi
  cd ../../
}

stringContain() { case $2 in *$1* ) return 0;; *) return 1;; esac ;}

echo "Cleaning previous build & packages..."
rm -rf build
rm -rf artifacts
mkdir artifacts

echo "Compiling..."

cd src

pub linux-arm64 $1
pub linux-x64 $1
pub win-arm64 $1
pub win-x64 $1
pub osx-arm64 $1
pub osx-x64 $1

cd ../
echo "Packaging builds..."

pack linux-arm64 $1
pack linux-x64 $1
pack win-arm64 $1
pack win-x64 $1
pack osx-arm64 $1
pack osx-x64 $1

mv build/linux-x64/appsettings.json artifacts/appsettings.json

echo "Complete. You can find builds for all 6 OSes in build/. Pre-compressed archives are in artifacts/."

if [ $2 != "false" ]; then
  read -n1 -r -p "Press any key to exit."  
fi