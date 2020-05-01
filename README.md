# Protagonist

(WIP) A collection of separate dotnet core applications that together form the basis of the new DLCS platform.

## Projects

There are a number of shared projects and entry point applications that use these shared projects, detailed below:

### Shared

* DLCS.Core - General non-domain specific utilities and exceptions.
* DLCS.Model - DLCS models and repository interfaces.
* DLCS.Repository - Repository implementations.
* DLCS.Test.Helpers - Classes to assist in testing.
* DLCS.Web - Classes that are aware of HTTP pipeline (e.g. request/response classes)
* IIIF - For parsing and processing IIIF requests.

In addition to the above there are a number of *.Tests classes for automated tests.

### Entry Points

* Thumbs - simplified handling of thumbnail requests.
* Engine - handles ingest requests via HTTP request (for image) and queue listeners (image and A/V).