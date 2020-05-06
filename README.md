# Tools.HttpProxyAndAudit

![Continous integration build and publish](https://github.com/swisschain/Tools.HttpProxyAndAudit/workflows/Continous%20integration%20build%20and%20publish/badge.svg)

docker image: [![docker image](https://img.shields.io/docker/v/swisschains/tools-http-proxy-and-audit?sort=semver)](https://hub.docker.com/repository/docker/swisschains/tools-http-proxy-and-audit)


# Environment variables

`ElasticsearchLogs__NodeUrls__1` = "http://elasticsearch.elk-logs.svc.cluster.local:9200"

`ConsoleOutputLogLevel` = "Error"

`DownstreamScheme`: "https",

`DownstreamHost`: "apiv2.lykke.com",

`SESSION_SERVICE_URL`: Url to api of lykke session server. Optional.
