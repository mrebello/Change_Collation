# Scripts para alteração de COLLATION em bancos SQL SERVER
## Objetivo
Fazer a alteração do COLLATION default do banco de dados e de todos os campos que possuem o collation default para um novo collation.
## Cuidados a serem tomados
<b>BACKUP</b> - Os scripts fazem _drop_ de restrições e chaves primárias, e depois recriam as mesmas.

<b>Acesso exclusivo</b> - Para fazer a alteração na base, é necessário acesso exclusivo a base, e durante esse processo não é possível deixar a base disponível para os clientes.

## Descrição
Precisei fazer a altertação do COLLATION default de um banco de dados com quase 5000 campos divididos em mais de 500 tabelas.

Ao executar o comando

	alter database nome_do_banco collate SQL_Latin1_General_CP1_CI_AI;
	go


apareceram muitos erros que impediam a alteração.

O maior problema era com as _constrains_ e com as chaves primárias.
Criei então alguns _scripts_ em C# para auxiliar a tarefa.

Para facilitar a vida dos 'não-programadores', disponibilizo aqui os scripts e uma forma fácil de executá-los sem a necessidade de conhecer C#. Para evitar problemas com opções não-usuais na criação dos scripts SQL, utilizei os objetos DMO do SQL Server que estão disponíveis no SQL Sercer Managment Studio.

Foram usadas as seguintes DLLs do DMO:

- Microsoft.SqlServer.Smo.dll
- Microsoft.SqlServer.Management.Sdk.Sfc.dll
- Microsoft.SqlServer.ConnectionInfo.dll
- Microsoft.SqlServer.SqlEnum.dll

que normalmente estão em "C:\Program Files (x86)\Microsoft SQL Server Management Studio 18\Common7\IDE\".

Caso possua elas em outra pasta, altere o nome da pasta nos arquivos ".bat".

Para a execução, deixo como recomendação utilizar o [cscs.exe](https://github.com/oleg-shilo/cs-script/blob/master/bin/linux/ubuntu/build/cs-script_3.27.5.0/cscs.exe), que permite executar scripts em C# sem a necessidade de instalação do Visual Studio. Os arquivos .bat já estão preparados para a execução através do cscs.exe.

## Passos para alteração do COLLATION
1. Verificação da possibilidade de alteração
2. Criar TODOS os scripts
3. Executar DROPs
4. Fazer a alteração do collation default
5. Executar scripts para alteração das tabelas
6. Executar scripts para recriação das restrições
7. Extra - recompilar todas as funções e procedimentos

### 1. Verificação da possibilidade de alteração
Ao alterar o COLLATION para um menos restritivo (como _AS_CI para _AI_CI por exemplo) deve-se primeiramente garantir que os dados permitem essa alteração.
Caso você possua, por exemplo, uma tabela onde o nome seja uma chave única, e possua um registro com o nome 'Joao' e outro com 'JOÃO', não será possível fazer a alteração de _AS_CI para _AI_CI, pois no novo COLLATION os valores são iguais.

### 2. Criar TODOS os scripts
Os scripts SQL são criados a partir da base existente, então é necessário executar a criação dos scripts __ANTES DA EXECUÇÃO DOS DROPs__, pois não é possível gerar o script para gerar as constrains após excluí-las do banco.

Para a geração, é necessário fazer a alteração dos parâmetros de conexão nos arquivos .cs. Basta alterar o nome do _host_, _username_ e _password_ na linha 13/14 dos arquivos (pode usar o bloco de notas mesmo).
Após a alteração, basta executar os .bat's para criar os scripts sql:

	C:\x\Change_Collation>Drops > drops.sql
	C:\x\Change_Collation>Collations > collations.sql
	C:\x\Change_Collation>Creates > creates.sql

### 3. Executar DROPs
Para o SQL Server aceitar a alteração do collation do banco de dados, ele primeiro verifica se existem impedimentos quanto às restrições (constrains) e funções/procedimentos (functions e store procedures) no banco de dados.
O maior problema normalmente é quanto as restrições (constrains e primary keys).

A solução encontrada foi excluir (drop) e depois recriar todas as restrições no banco. Este não é um procedimento simples nem rápido, principalmente para bases grandes. Então não deve ser feito sem as devidas precauções.

Os scripts aqui apresentados __NÃO__ estão fazendo o _drop_ das funções e procedimentos que impedem a alteração. No meu caso, como eram apenas 5 funções nessa condição, gerei os scripts para a recriação manualmente pelo SSMS. Uma alteração nos scripts para o drop e create das funções é simples - exceto a seleção de drop/create para apenas as funções que realmente necessitam ser recriadas.

### 4. Fazer a alteração do collation default
Para fazer a alteração do collation default, é necessário acesso exclusivo à base.
A sequência de comandos fica:

	alter database [database] set single_user with rollback immediate;
	go
	alter database [database] collate SQL_Latin1_General_CP1_CI_AI;
	go
	alter database [database] set multi_user;
	go

__RECOMENDAÇÃO:__ Mudar a base para _single_user_ antes de fazer os drops, e voltar para _multi_user_ após a recriação das constrains.
### 5. Executar scripts para alteração das tabelas
Como a alteração do collation default não modifica os campos existentes nas tabelas, é necessário fazer a alteração campo a campo.
O script criado pelo collations.cs faz a alteração em todos os campos texto (char, varchar, nchar, nvarchar, text, ...) que possuirem collation igual ao default do banco na hora da criação dos scripts.

### 6. Executar scripts para recriação das restrições
Esse é o passo mais demorado, pois como o passo 3 excluiu todas os índices e chaves do banco, o script criado pelo _creates.cs_ recria todos os índices da base. Em bases grandes isso pode levar bastante tempo.

### 7. Extra - recompilar todas as funções e procedimentos
Após as alterações, é recomendável também _recompilar_ todos os procedimentos e funções, pois esse passo não é verificado pelo SQL Server e pode gerar erros na hora de executar os procedimentos e funções.
O script SQL abaixo _tenta_ recompilar todos os procedimentos e funções do banco e informa os erros onde houverem.
Os erros podem ser gerados por collations diferentes utilizados em concatenação de campos, por exemplo, quando existirem collations explícitos nos procedimentos.

    declare cur cursor for 
      select quotename(s.name) + '.' + quotename(o.name) as procname
        from sys.sql_modules m
          inner join sys.objects o on m.object_id=o.object_id
          inner join sys.schemas s on o.schema_id = s.schema_id
    declare @procname sysname;
    open cur
    while 1=1 begin
      fetch next from cur into @procname;
      if @@FETCH_STATUS<>0 BREAK
      BEGIN TRY  
        exec sp_refreshsqlmodule @procname;
      END TRY  
      BEGIN CATCH  
        print 'ERRO em ' + @procname + ': ' + ERROR_MESSAGE()
      END CATCH 
    end
    close cur
    deallocate cur




