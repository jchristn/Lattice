#!/bin/bash
docker run -e MYSQL_ROOT_PASSWORD=password -p 3306:3306 -d mysql
