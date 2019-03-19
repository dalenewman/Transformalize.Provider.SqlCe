### Overview

This is a `SqlCe` provider for Transformalize using [Microsoft.SqlServer.Compact](https://www.nuget.org/packages/Microsoft.SqlServer.Compact).  

It ships with Transformalize CLI.

### Write Usage

```xml
<add name='Bogus' mode='init' flatten='false'>
  <connections>
    <add name='input' provider='bogus' seed='1' />
    <add name='output' provider='sqlce' file='c:\temp\junk.sdf' />
  </connections>
  <entities>
    <add name='Contact' size='1000'>
      <fields>
        <add name='Identity' type='int' primary-key='true' />
        <add name='FirstName' />
        <add name='LastName' />
        <add name='Stars' type='byte' min='1' max='5' />
        <add name='Reviewers' type='int' min='0' max='500' />
      </fields>
    </add>
  </entities>
</add>
```

This writes 1000 rows of bogus data to a SqlCe database.

### Read Usage

```xml
<add name='Bogus'>
  <connections>
    <add name='input' provider='sqlce' file='c:\temp\junk.sdf' />
    <add name='output' provider='internal' />
  </connections>
  <entities>
    <add name='BogusContactTable' alias='Contact' page='1' size='10'>
      <order>
        <add field='A5' />
      </order>
      <fields>
        <add name='A5' alias='Identity' type='int' />
        <add name='A6' alias='FirstName' />
        <add name='A7' alias='LastName' />
        <add name='A8' alias='Stars' type='byte' />
        <add name='A9' alias='Reviewers' type='int' />
      </fields>
    </add>
  </entities>
</add>```

This reads 10 rows of bogus data from a SqlCe database:

<pre>
<strong>Identity,FirstName,LastName,Stars,Reviewers</strong>
1,Justin,Konopelski,3,153
2,Eula,Schinner,2,41
3,Tanya,Shanahan,4,412
4,Emilio,Hand,4,469
5,Rachel,Abshire,3,341
6,Doyle,Beatty,4,458
7,Delbert,Durgan,2,174
8,Harold,Blanda,4,125
9,Willie,Heaney,5,342
10,Sophie,Hand,2,176</pre>