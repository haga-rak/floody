 # floody

**This repository c**ontains a combination of an HTTP client (floody) and an HTTP server (floodys) 
that can be used to test the performance of an HTTP proxy.
Without the intermediary, this configuration can generate 300K requests per second.

```
+---------+       +-----------+       +-----------+
|  floody |------>|   Proxy   |------>|  floodys  |
| (Client)|       | (to test) |       | (Server)  |
+---------+       +-----------+       +-----------+
```

## Build and run 

This project need .NET 9 SDK to build and run.

Clone the repository 
```bash
git clone https://github.com/haga-rak/floody.git
```

The build script (cmd and bash) can be used to bootstrap quickly a test. 
A workflow will build and run automatically the server (on a free port) and the client according to the provided options. 

```bash
build benchmarkhttps "floody: <floody-command-line-options>"
```
Replace benchmarkhttps with benchmarkhttp to test plain HTTP.
The following command tests the proxy at 127.0.0.1:44344 with 15 concurrent connections and 10 seconds duration.

```bash
build benchmarkhttps "floody: -d 10 -c 15 -x 127.0.0.1:44344"
```

floody command line options:
```
Description:
  A simple http load test tool supporting proxy

Usage:
  floody <uri> [options]

Arguments:
  <uri>

Options:
  -X, --method <method>                                Method to used [default: GET]
  -c, --concurrent-connection <concurrent-connection>  Concurrent connection count to the remote [default: 8]
  -x, --proxy <proxy>                                  Address of HTTP proxy
  -r, --request-body-length <request-body-length>      Request body length
  -l, --response-body-length <response-body-length>    Response body length (sends `length` as query string, works 
                                                       only when used with floodys)
  -H, --header <header>                                Additional HTTP headers []
  -o, --output-file <output-file>                      Output benchmark result into a json file
  -d, --duration <duration>                            Test duration (unit accepted: ms, s, mn, h) [default: 00:00:30]
  -w, --warm-up <warm-up>                              Warm up duration (unit accepted: ms, s, mn, h) [default: 
                                                       00:00:05]
  --version                                            Show version information
  -?, -h, --help                                       Show help and usage information
```






