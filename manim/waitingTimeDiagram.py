from manim import *

DEFAULT_FONT_SIZE = 30
Text.set_default(font="sans-serif", font_size=DEFAULT_FONT_SIZE)

class WaitingTimeDiagram(Scene):
    def construct(self):
      self.camera.background_color = "#444444"
      
      values=[0, 0, 0, 0, 0, 0, 0, 0, 0]
      final_values = [0, 31, 28, 25, 22, 17, 15, 13, 12]
      bar_names = ["-3", "-2", "-1", "0", "1", "2", "3", "4", "5"]
      chart = BarChart(
          values,
          bar_names=bar_names,
          y_range=[0, 40, 10],
          y_length=5.5,
          x_length=10,
          bar_fill_opacity=1,
          y_axis_config={
            "font_size": 24,
            "label_constructor": Text,   
          },
          x_axis_config={
            "font_size": 30,
            "label_constructor": Text,
            "include_ticks": False,
          },
          bar_colors=[ORANGE, GREEN]
      ).shift(UP*0.2)
      xLabel = chart.get_x_axis_label(Text("# Drivers available")).shift(LEFT*5.5 + DOWN*1.2)
      yLabel = chart.get_y_axis_label(Text("Average waiting time (minutes)", font_size=26)).shift(LEFT*3.8 + DOWN*3.1).rotate(90 * DEGREES)
      # axisLabels = chart.get_axis_labels(Text("Available drivers"), Text("Waiting time (minutes)"))
      self.add(chart, xLabel)
      self.add(chart, yLabel)
      self.add(chart)
      self.wait(2)

      self.play(chart.animate.change_bar_values(final_values), run_time=3)
      barLabels = chart.get_bar_labels(font_size=24, label_constructor=Text)[1:]
      self.play(FadeIn(barLabels)) 

      self.wait(3)
    
