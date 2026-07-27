## ADDED Requirements

### Requirement: MongoDB connection configuration
The system SHALL read MongoDB connection string from `appsettings.json` under the key `MongoDb:ConnectionString`. The default connection string SHALL be `mongodb://localhost:27017`.

#### Scenario: Default connection
- **WHEN** no connection string is configured in appsettings.json
- **THEN** the system connects to `mongodb://localhost:27017`

#### Scenario: Custom connection
- **WHEN** `MongoDb:ConnectionString` is set to `mongodb://custom-host:27017`
- **THEN** the system connects to `mongodb://custom-host:27017`

### Requirement: Database name configuration
The system SHALL read the database name from `appsettings.json` under the key `MongoDb:DatabaseName`. The default database name SHALL be `databank`.

#### Scenario: Default database name
- **WHEN** no database name is configured
- **THEN** the system uses `databank` as the database name

### Requirement: DataBankEntry collection schema
The system SHALL store DataBankEntry documents in a MongoDB collection named `DataBankEntry`. Each document SHALL contain fields: `_id` (string, the entry ID), `Key` (string), `Value` (string), `Locale` (string), `Source` (embedded document with Format, File, Path, Encoding), `Metadata` (embedded document with Comment, RcId, RcDefine, IsBehavioral, FormatSpecifiers, DoNotTranslate).

#### Scenario: Entry document structure
- **WHEN** a DataBankEntry is inserted
- **THEN** the document contains all required fields matching the LocalizedStringEntry model

### Requirement: DataBankMetadata collection schema
The system SHALL store DataBankMetadata documents in a collection named `DataBankMetadata`. Each document SHALL contain fields: `_id` (string), `Version` (int), `Generated` (string), `EntryCount` (int).

#### Scenario: Metadata document structure
- **WHEN** metadata is inserted
- **THEN** the document contains version, generation timestamp, and entry count

### Requirement: TranslationSession collection schema
The system SHALL store TranslationSession documents in a collection named `TranslationSession`. Each document SHALL contain fields: `_id` (ObjectId, auto-generated), `SessionName` (string), `SourceLocale` (string), `TargetLocale` (string), `Status` (string: pending/in-progress/completed), `CreatedAt` (DateTime), `UpdatedAt` (DateTime), `EntryIds` (array of strings).

#### Scenario: Session document structure
- **WHEN** a TranslationSession is created
- **THEN** the document contains all required fields with auto-generated `_id`

### Requirement: Indexes on DataBankEntry collection
The system SHALL create the following indexes on the `DataBankEntry` collection: unique index on `Key`, index on `Locale`, compound index on `Source.Format`, index on `Metadata.DoNotTranslate`.

#### Scenario: Key index exists
- **WHEN** the application starts
- **THEN** a unique index exists on `DataBankEntry.Key`

#### Scenario: Locale index exists
- **WHEN** the application starts
- **THEN** an index exists on `DataBankEntry.Locale`

#### Scenario: Format index exists
- **WHEN** the application starts
- **THEN** a compound index exists on `DataBankEntry.Source.Format`

### Requirement: Indexes on TranslationSession collection
The system SHALL create indexes on `TranslationSession`: index on `Status`, compound index on `SourceLocale` and `TargetLocale`.

#### Scenario: Status index exists
- **WHEN** the application starts
- **THEN** an index exists on `TranslationSession.Status`

### Requirement: Repository interface definition
The system SHALL define an `IDataBankRepository` interface with methods: `GetAllAsync`, `GetByIdAsync`, `GetByKeyAsync`, `GetByLocaleAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`, `GetMetadataAsync`, `UpdateMetadataAsync`.

#### Scenario: Interface is defined
- **WHEN** the DataBank.Api project compiles
- **THEN** `IDataBankRepository` exists with all specified methods

### Requirement: MongoDB repository implementation
The system SHALL implement `MongoDataBankRepository` class that satisfies `IDataBankRepository`. The implementation SHALL use `IMongoCollection<T>` for each collection type.

#### Scenario: Repository is registered in DI
- **WHEN** the application starts
- **THEN** `IDataBankRepository` is registered in the dependency injection container with `MongoDataBankRepository` as the implementation
