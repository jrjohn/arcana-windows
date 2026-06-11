# Changelog

## [1.1.0](https://github.com/jrjohn/arcana-windows/compare/v1.0.0...v1.1.0) (2026-06-11)


### Features

* add dotnet-sonarscanner CI (Dockerfile.sonar + docker-compose.sonar.yml) ([c6d6ca8](https://github.com/jrjohn/arcana-windows/commit/c6d6ca8c97a7919aef928dae80119c2dedcfcc03))


### Bug Fixes

* **ci:** de-plant hardcoded SonarQube token from Jenkinsfile.dotnet ([a223e57](https://github.com/jrjohn/arcana-windows/commit/a223e578f28254f83c81900e229fcbdc0c25e364))
* **ci:** harden Jenkinsfile.windows test + arch-qube gates ([86a3459](https://github.com/jrjohn/arcana-windows/commit/86a34596dba1eb28859d5783c5cd6e5e6dda539c))
* **ci:** tag build-N atomically via build.tags ([6402879](https://github.com/jrjohn/arcana-windows/commit/6402879c2f1f9a30a5cfdc35d472151f0d73556d))
* move --results-directory before -- separator in dotnet test ([21112ba](https://github.com/jrjohn/arcana-windows/commit/21112ba1532f285445a902195da9a28489462736))
* rename WindowService impl to resolve circular base class dependency ([83ac35b](https://github.com/jrjohn/arcana-windows/commit/83ac35bcf8a61ad8556d4fccdff8225f127d485c))
* resolve compile errors - rename impl classes and constructors ([3b02986](https://github.com/jrjohn/arcana-windows/commit/3b02986a83161debcf1125bea14664dc73d55a00))
* resolve MessageBus timeout and AuthService test failures ([76b7570](https://github.com/jrjohn/arcana-windows/commit/76b75701926667008f51c33f8963e327c8370992))
* resolve remaining compile errors in Infrastructure and tests ([c8adc66](https://github.com/jrjohn/arcana-windows/commit/c8adc66e6e3a0995a2e0bcf988de6629158b6650))
* **sonar:** resolve 12 remaining dotnet-app issues ([9ecdc07](https://github.com/jrjohn/arcana-windows/commit/9ecdc07f5176d3eafbc9d073462934ee6199bd01))
* **sonar:** resolve 214 issues + 6 hotspots in dotnet-app ([58af29c](https://github.com/jrjohn/arcana-windows/commit/58af29c82d500fe373633175e716a2d223444ad9))
* **sonar:** resolve last dotnet-app issue CS8625 null literal ([ebd9931](https://github.com/jrjohn/arcana-windows/commit/ebd9931c60030915bbc04d370dd4835d781feded))
* **test:** cross-platform path assertion + timeout race condition ([53c5ca3](https://github.com/jrjohn/arcana-windows/commit/53c5ca3076f9f40b155abfef5c9c01b2e1e4be37))
* TestEvent must inherit ApplicationEventBase (implement IApplicationEvent) ([6b0ab5b](https://github.com/jrjohn/arcana-windows/commit/6b0ab5b2d9c75a2cf84ef5201060270329ff47ef))
* **test:** use Path.GetFullPath for cross-platform path normalization ([e44eb0e](https://github.com/jrjohn/arcana-windows/commit/e44eb0e82fdbac6a8e44146bef0a0fa3182b0f37))
* UnitOfWorkFactory → UnitOfWorkFactoryImpl (ambiguous CS0104) ([ee65519](https://github.com/jrjohn/arcana-windows/commit/ee65519d8c2cbb64b0fc75f3ec2b07fa152b5a6b))
* use opencover format for coverage reports (XPlat Code Coverage) ([b842965](https://github.com/jrjohn/arcana-windows/commit/b842965ff221ad049d2ed682a4a54cae600bd118))
* use useradd instead of adduser (not available in dotnet sdk slim) ([fa5d7b0](https://github.com/jrjohn/arcana-windows/commit/fa5d7b044bbaea7b0e14bbf938bef2b28a0b38b9))
