nuget pack Transformalize.Provider.SqlCe.nuspec -OutputDirectory "c:\temp\modules"
nuget pack Transformalize.Provider.SqlCe.Autofac.nuspec -OutputDirectory "c:\temp\modules"

REM nuget push "c:\temp\modules\Transformalize.Provider.SqlCe.0.8.1-beta.nupkg" -source https://api.nuget.org/v3/index.json
REM nuget push "c:\temp\modules\Transformalize.Provider.SqlCe.Autofac.0.8.1-beta.nupkg" -source https://api.nuget.org/v3/index.json






