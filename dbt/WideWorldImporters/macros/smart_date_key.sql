{% macro smart_date_key(date) %}
    YEAR( {{ date }}) * 10000 + MONTH({{ date }}) * 100 + DAY({{ date }})
{% endmacro %}