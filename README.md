 # floody

**This repository c**ontains a combination of an HTTP client (floody) and an HTTP server (floodys) 
that can be used to test the performance of an HTTP proxy.

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

The build script (cmd and bash), created with Bulleyes.Target, can be used to bootstrap a test quickly. 

Basic syntax:

```bash
build benchmarkhttps "floody-options: <floody-command-line-options>"
```

Replace `benchmarkhttps` with `benchmarkhttp` to test in plain HTTP. 
The following command tests the proxy at 127.0.0.1:44344 with 15 concurrent connections and 10 seconds duration.

```bash
build benchmarkhttps "floody: -d 10 -c 15 -x 127.0.0.1:44344"
```

## Details about apps 

### floody 


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






