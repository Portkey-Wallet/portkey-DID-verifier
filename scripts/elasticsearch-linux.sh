#!/bin/bash

set -x

groupadd elastic
useradd -g elastic elastic
pgrep elasticsearch | xargs kill -9
wget https://artifacts.elastic.co/downloads/elasticsearch/elasticsearch-7.15.1-linux-x86_64.tar.gz
tar -zxf elasticsearch-7.15.1-linux-x86_64.tar.gz -C /opt

chown -R elastic. /opt/elasticsearch-7.15.1

su - elastic -c "/opt/elasticsearch-7.15.1/bin/elasticsearch -d"

sleep 30
curl http://127.0.0.1:9200
