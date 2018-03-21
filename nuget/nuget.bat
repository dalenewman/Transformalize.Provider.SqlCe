nuget pack Transformalize.Provider.SqlCe.nuspec -OutputDirectory "c:\temp\modules"
nuget pack Transformalize.Provider.SqlCe.Autofac.nuspec -OutputDirectory "c:\temp\modules"

nuget push "c:\temp\modules\Transformalize.Provider.SqlCe.0.3.5-beta.nupkg" -source https://api.nuget.org/v3/index.json
nuget push "c:\temp\modules\Transformalize.Provider.SqlCe.Autofac.0.3.5-beta.nupkg" -source https://api.nuget.org/v3/index.json






