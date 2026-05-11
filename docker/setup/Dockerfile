FROM mcr.microsoft.com/mssql-tools

RUN apt-get update \
    && apt-get install -y --no-install-recommends mysql-client postgresql-client \
    && rm -rf /var/lib/apt/lists/*

COPY setup.sh /setup.sh
RUN chmod +x /setup.sh

CMD ["/setup.sh"]
