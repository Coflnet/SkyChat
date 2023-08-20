VERSION=0.1.0

docker run --rm -v "${PWD}:/local" --network host -u $(id -u ${USER}):$(id -g ${USER})  openapitools/openapi-generator-cli generate \
-i http://localhost:5010/swagger/v1/swagger.json \
-g csharp \
-o /local/out --additional-properties=packageName=Coflnet.Sky.Chat.Client,packageVersion=$VERSION,licenseId=MIT

cd out
sed -i 's/GIT_USER_ID/Coflnet/g' src/Coflnet.Sky.Chat.Client/Coflnet.Sky.Chat.Client.csproj
sed -i 's/GIT_REPO_ID/SkyChat/g' src/Coflnet.Sky.Chat.Client/Coflnet.Sky.Chat.Client.csproj
sed -i 's/>OpenAPI/>Coflnet/g' src/Coflnet.Sky.Chat.Client/Coflnet.Sky.Chat.Client.csproj

dotnet pack
cp src/Coflnet.Sky.Chat.Client/bin/Debug/Coflnet.Sky.Chat.Client.*.nupkg ..
