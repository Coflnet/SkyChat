VERSION=0.2.1

docker run --rm -v "${PWD}:/local" --network host -u $(id -u ${USER}):$(id -g ${USER})  openapitools/openapi-generator-cli generate \
-i http://localhost:5010/swagger/v1/swagger.json \
-g csharp \
-o /local/out --additional-properties=packageName=Coflnet.Sky.Chat.Client,packageVersion=$VERSION,licenseId=MIT,targetFramework=net6.0

cd out
path=src/Coflnet.Sky.Chat.Client/Coflnet.Sky.Chat.Client.csproj
sed -i 's/GIT_USER_ID/Coflnet/g' $path
sed -i 's/GIT_REPO_ID/SkyChat/g' $path
sed -i 's/>OpenAPI/>Coflnet/g' $path
sed -i 's@annotations</Nullable>@annotations</Nullable>\n    <PackageReadmeFile>README.md</PackageReadmeFile>@g' $path
sed -i 's@Remove="System.Web" />@Remove="System.Web" />\n    <None Include="../../../../README.md" Pack="true" PackagePath="\"/>@g' $path

dotnet pack
cp src/Coflnet.Sky.Chat.Client/bin/Release/Coflnet.Sky.Chat.Client.*.nupkg ..
